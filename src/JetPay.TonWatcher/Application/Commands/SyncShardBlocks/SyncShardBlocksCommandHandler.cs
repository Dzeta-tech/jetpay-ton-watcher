using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Domain.Entities;
using MediatR;
using TonSdk.Adnl.LiteClient;

namespace JetPay.TonWatcher.Application.Commands.SyncShardBlocks;

public class SyncShardBlocksCommandHandler(
    IShardBlockRepository shardBlockRepository,
    ILiteClientService liteClientService,
    ILogger<SyncShardBlocksCommandHandler> logger)
    : IRequestHandler<SyncShardBlocksCommand, SyncShardBlocksResult>
{
    public async Task<SyncShardBlocksResult> Handle(SyncShardBlocksCommand request, CancellationToken cancellationToken)
    {
        int totalBlocksAdded = 0;

        try
        {
            MasterChainInfoExtended masterchainInfo = await liteClientService.GetMasterChainInfoAsync();
            BlockIdExtended[] shards = await liteClientService.GetShardsAsync(masterchainInfo.LastBlockId);

            if (shards == null || shards.Length == 0)
            {
                return new SyncShardBlocksResult { Success = true, BlocksAdded = 0 };
            }

            foreach (BlockIdExtended shard in shards)
            {
                int blocksAdded = await ProcessShard(shard, cancellationToken);
                totalBlocksAdded += blocksAdded;
            }

            return new SyncShardBlocksResult
            {
                Success = true,
                BlocksAdded = totalBlocksAdded
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error syncing shard blocks");
            return new SyncShardBlocksResult { Success = false };
        }
    }

    async Task<int> ProcessShard(BlockIdExtended shard, CancellationToken cancellationToken)
    {
        long maxSeqno = await shardBlockRepository.GetMaxSeqnoAsync(shard.Shard, cancellationToken);

        if (maxSeqno == 0)
            maxSeqno = shard.Seqno - 1;

        int blocksAdded = 0;
        for (long seqno = maxSeqno + 1; seqno <= shard.Seqno; seqno++)
        {
            ShardBlock shardBlock = ShardBlock.Create(shard.Workchain, shard.Shard, seqno);
            await shardBlockRepository.AddAsync(shardBlock, cancellationToken);
            blocksAdded++;
        }

        return blocksAdded;
    }
}

