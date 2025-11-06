using BloomFilter;
using JetPay.TonWatcher.Domain.Entities;
using JetPay.TonWatcher.Domain.Events;
using JetPay.TonWatcher.Domain.ValueObjects;
using JetPay.TonWatcher.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ton.LiteClient;
using Ton.LiteClient.Models;

namespace JetPay.TonWatcher.Application.Commands;

public record ProcessShardBlockCommand : IRequest<ProcessShardBlockResult>
{
    public required Guid ShardBlockId { get; init; }
}

public record ProcessShardBlockResult
{
    public bool Success { get; init; }
    public int TransactionsFound { get; init; }
    public List<TransactionInfo> Transactions { get; init; } = [];
}

public class ProcessShardBlockCommandHandler(
    ApplicationDbContext dbContext,
    LiteClient liteClient,
    IBloomFilter bloomFilter,
    IMediator mediator,
    ILogger<ProcessShardBlockCommandHandler> logger)
    : IRequestHandler<ProcessShardBlockCommand, ProcessShardBlockResult>
{
    public async Task<ProcessShardBlockResult> Handle(ProcessShardBlockCommand request,
        CancellationToken cancellationToken)
    {
        ShardBlock? shardBlock = await dbContext.ShardBlocks.FindAsync([request.ShardBlockId], cancellationToken);
        if (shardBlock is null)
        {
            logger.LogWarning("ShardBlock {Id} not found", request.ShardBlockId);
            return new ProcessShardBlockResult { Success = false };
        }

        if (shardBlock.IsProcessed) return new ProcessShardBlockResult { Success = true, TransactionsFound = 0 };

        List<TransactionInfo> foundTransactions = [];

        try
        {
            BlockId blockId;
            try
            {
                blockId = await liteClient.LookupBlockAsync(
                    shardBlock.Workchain,
                    shardBlock.Shard,
                    shardBlock.Seqno,
                    cancellationToken);
            }
            catch
            {
                logger.LogWarning("Block {Shard}:{Seqno} not found, will retry later",
                    shardBlock.Shard, shardBlock.Seqno);
                return new ProcessShardBlockResult { Success = false };
            }

            BlockTransactions blockTransactions =
                await liteClient.ListBlockTransactionsAsync(blockId, 100000, cancellationToken: cancellationToken);

            if (blockTransactions.Transactions.Count == 0)
            {
                shardBlock.MarkAsProcessed();
                await dbContext.SaveChangesAsync(cancellationToken);
                return new ProcessShardBlockResult { Success = true, TransactionsFound = 0 };
            }

            foreach (BlockTransaction tx in blockTransactions.Transactions)
            {
                if (!await bloomFilter.ContainsAsync(tx.Account.Hash))
                    continue;

                TrackedAddress? trackedAddress = await dbContext.TrackedAddresses.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Address == tx.Account, cancellationToken);

                if (trackedAddress is not { IsTrackingActive: true })
                    continue;

                TransactionInfo txInfo = new()
                {
                    Address = tx.Account,
                    TxHash = Convert.ToBase64String(tx.Hash),
                    LogicalTime = tx.Lt
                };

                foundTransactions.Add(txInfo);

                await mediator.Publish(new TransactionFoundEvent
                {
                    Address = tx.Account,
                    TxHash = txInfo.TxHash,
                    Lt = txInfo.LogicalTime
                }, cancellationToken);
            }

            shardBlock.MarkAsProcessed();
            await dbContext.SaveChangesAsync(cancellationToken);

            return new ProcessShardBlockResult
            {
                Success = true,
                TransactionsFound = foundTransactions.Count,
                Transactions = foundTransactions
            };
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Timeout processing shard {Shard}:{Seqno}, will retry later",
                shardBlock.Shard, shardBlock.Seqno);
            return new ProcessShardBlockResult { Success = false };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing shard {Shard}:{Seqno}, will retry later",
                shardBlock.Shard, shardBlock.Seqno);
            return new ProcessShardBlockResult { Success = false };
        }
    }
}