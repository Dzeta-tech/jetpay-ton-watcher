using BloomFilter;
using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Domain.Entities;
using JetPay.TonWatcher.Domain.Events;
using JetPay.TonWatcher.Domain.ValueObjects;
using MediatR;
using TonSdk.Adnl.LiteClient;
using TonSdk.Core;
using TransactionId = TonSdk.Adnl.LiteClient.TransactionId;

namespace JetPay.TonWatcher.Application.Commands.ProcessShardBlock;

public class ProcessShardBlockCommandHandler(
    IShardBlockRepository shardBlockRepository,
    ITrackedAddressRepository trackedAddressRepository,
    ILiteClientService liteClientService,
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
            BlockIdExtended? blockId = await liteClientService.LookupBlockAsync(
                shardBlock.Workchain,
                shardBlock.Shard,
                shardBlock.Seqno);

            if (blockId == null)
            {
                logger.LogWarning("Block {Shard}:{Seqno} not found, will retry later",
                    shardBlock.Shard, shardBlock.Seqno);
                return new ProcessShardBlockResult { Success = false };
            }

            ListBlockTransactionsResult transactionsResult =
                await liteClientService.GetBlockTransactionsAsync(blockId, 100000);

            if (transactionsResult.TransactionIds == null || transactionsResult.TransactionIds.Length == 0)
            {
                shardBlock.MarkAsProcessed();
                await shardBlockRepository.UpdateAsync(shardBlock, cancellationToken);
                return new ProcessShardBlockResult { Success = true, TransactionsFound = 0 };
            }

            foreach (TransactionId? tx in transactionsResult.TransactionIds)
            {
                if (!await bloomFilter.ContainsAsync(tx.Account))
                    continue;

                Address txAddress = new(shardBlock.Workchain, tx.Account);
                string addressStr = txAddress.ToBase64(bounceable: false, testOnly: false, urlSafe: true);

                TrackedAddress? trackedAddress = await trackedAddressRepository
                    .GetByAccountAsync(shardBlock.Workchain, tx.Account, cancellationToken);

                if (trackedAddress is not { IsTrackingActive: true })
                    continue;

                TransactionInfo txInfo = new()
                {
                    Address = addressStr,
                    TxHash = Convert.ToBase64String(tx.Hash),
                    LogicalTime = (ulong)tx.Lt,
                    AccountHash = tx.Account,
                    Workchain = shardBlock.Workchain
                };

                foundTransactions.Add(txInfo);

                // Publish domain event
                await mediator.Publish(new TransactionFoundEvent
                {
                    Address = addressStr,
                    TxHash = txInfo.TxHash,
                    Lt = txInfo.LogicalTime,
                    DetectedAt = DateTime.UtcNow
                }, cancellationToken);

                logger.LogInformation("Transaction found for {Address}, TxHash: {Hash}, Lt: {Lt}",
                    addressStr, txInfo.TxHash, txInfo.LogicalTime);
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