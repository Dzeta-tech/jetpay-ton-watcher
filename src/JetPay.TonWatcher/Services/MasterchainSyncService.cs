using JetPay.TonWatcher.Data;
using JetPay.TonWatcher.Data.Models;
using Microsoft.EntityFrameworkCore;
using TonSdk.Adnl.LiteClient;

namespace JetPay.TonWatcher.Services;

public class MasterchainSyncService(
    ILogger<MasterchainSyncService> logger,
    LiteClientProvider liteClientProvider,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    readonly TimeSpan syncInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            MasterChainInfoExtended masterchainInfo = await liteClientProvider.GetMasterChainInfoAsync();
            
            // Get actual shards
            BlockIdExtended[] shards = await liteClientProvider.GetShardsAsync(masterchainInfo.LastBlockId);
            if (shards == null || shards.Length == 0)
                goto Delay;

            foreach (BlockIdExtended shard in shards)
                await ProcessShard(shard, dbContext);

            await dbContext.SaveChangesAsync(stoppingToken);

            Delay:
            await Task.Delay(syncInterval, stoppingToken);
        }
    }

    async Task ProcessShard(BlockIdExtended shard, ApplicationDbContext dbContext)
    {
        // Search for max seqno of this shard in database
        long maxSeqno = await dbContext.ShardBlocks.AsNoTracking().Where(x => x.Shard == shard.Shard)
            .OrderByDescending(x => x.Seqno).Select(x => x.Seqno).FirstOrDefaultAsync();
        
        if (maxSeqno == 0)
            maxSeqno = shard.Seqno - 1;

        // Add all missing blocks from maxSeqno to current shard.Seqno
        for (long seqno = maxSeqno + 1; seqno <= shard.Seqno; seqno++)
        {
            ShardBlock shardBlock = new()
            {
                Workchain = shard.Workchain,
                Shard = shard.Shard,
                Seqno = seqno
            };
            await dbContext.ShardBlocks.AddAsync(shardBlock);
        }
    }
}