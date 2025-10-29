using BloomFilter;
using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Domain.Entities;
using JetPay.TonWatcher.Domain.Events;
using JetPay.TonWatcher.Domain.ValueObjects;
using MediatR;
using Ton.LiteClient;
using Ton.LiteClient.Models;

namespace JetPay.TonWatcher.Application.Commands.ProcessShardBlock;

public class ProcessShardBlockCommandHandler(
    IShardBlockRepository shardBlockRepository,
    ITrackedAddressRepository trackedAddressRepository,
    LiteClient liteClient,
    IBloomFilter bloomFilter,
    IMediator mediator,
    ILogger<ProcessShardBlockCommandHandler> logger)
    : IRequestHandler<ProcessShardBlockCommand, ProcessShardBlockResult>
{
    public async Task<ProcessShardBlockResult> Handle(ProcessShardBlockCommand request,
        CancellationToken cancellationToken)
    {
        ShardBlock? shardBlock = await shardBlockRepository.GetByIdAsync(request.ShardBlockId, cancellationToken);
        if (shardBlock == null)
        {
            logger.LogWarning("ShardBlock {Id} not found", request.ShardBlockId);
            return new ProcessShardBlockResult { Success = false };
        }

        if (shardBlock.IsProcessed) return new ProcessShardBlockResult { Success = true, TransactionsFound = 0 };

        List<TransactionInfo> foundTransactions = new();

        try
        {
            BlockId blockId;
            try
            {
                blockId = await liteClient.LookupBlockAsync(
                    shardBlock.Workchain,
                    shardBlock.Shard,
                    (uint)shardBlock.Seqno,
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
                await shardBlockRepository.UpdateAsync(shardBlock, cancellationToken);
                return new ProcessShardBlockResult { Success = true, TransactionsFound = 0 };
            }

            foreach (BlockTransaction tx in blockTransactions.Transactions)
            {
                if (!await bloomFilter.ContainsAsync(tx.Account))
                    continue;

                TrackedAddress? trackedAddress = await trackedAddressRepository
                    .GetByAddressHashAsync(tx.Account, cancellationToken);

                if (trackedAddress is not { IsTrackingActive: true })
                    continue;

                string addressRaw = $"{blockId.Workchain}:{tx.AccountHex}";

                TransactionInfo txInfo = new()
                {
                    Address = addressRaw,
                    TxHash = Convert.ToBase64String(tx.Hash),
                    LogicalTime = tx.Lt,
                    AccountHash = tx.Account,
                    Workchain = shardBlock.Workchain
                };

                foundTransactions.Add(txInfo);

                // Publish domain event with address format
                await mediator.Publish(new TransactionFoundEvent
                {
                    Address = addressRaw,
                    TxHash = txInfo.TxHash,
                    Lt = txInfo.LogicalTime,
                    DetectedAt = DateTime.UtcNow
                }, cancellationToken);

                logger.LogInformation("Transaction found for {Address}, TxHash: {Hash}, Lt: {Lt}",
                    addressRaw, txInfo.TxHash, txInfo.LogicalTime);
            }

            shardBlock.MarkAsProcessed();
            await shardBlockRepository.UpdateAsync(shardBlock, cancellationToken);

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