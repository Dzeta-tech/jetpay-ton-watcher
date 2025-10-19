using JetPay.TonWatcher.Data;
using JetPay.TonWatcher.Data.Models;
using Microsoft.EntityFrameworkCore;
using TonSdk.Client;

namespace JetPay.TonWatcher.Services;

public class MasterchainSyncService(
    ILogger<MasterchainSyncService> logger,
    ITonClientFactory tonClientFactory,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    readonly TimeSpan syncInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            TonClient client = tonClientFactory.GetClient();
            MasterchainInformationResult? masterchainInfo = await client.GetMasterchainInfo();

            // Get actual shards
            ShardsInformationResult? shards = await client.Shards(masterchainInfo.Value.LastBlock.Seqno);
            if (!shards.HasValue)
                goto Delay;

            foreach (BlockIdExtended shard in shards.Value.Shards)
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

        // Process missed shard blocks
        for (long seqno = maxSeqno + 1; seqno < shard.Seqno; seqno++)
            await ProcessOldShardBlocks(shard, seqno, dbContext);

        // Process current shard block if needed
        if (maxSeqno < shard.Seqno)
            await ProcessShardBlock(shard, dbContext);
    }

    async Task ProcessOldShardBlocks(BlockIdExtended shard, long seqno, ApplicationDbContext dbContext) 
    {
        // Get shard transactions
        TonClient client = tonClientFactory.GetClient();
        var block = await client.LookUpBlock(shard.Workchain, shard.Shard, seqno);
        
        if (block is null)
            return;

        ShardBlock shardBlock = new()
        {
            Workchain = shard.Workchain,
            Shard = shard.Shard,
            Seqno = seqno,
            RootHash = block.RootHash,
            FileHash = block.FileHash
        };
        await dbContext.ShardBlocks.AddAsync(shardBlock);
        await dbContext.SaveChangesAsync(); // TODO: This is not the best way to do this, but it's the only way to get the id of the shard block
    }

    async Task ProcessShardBlock(BlockIdExtended shard, ApplicationDbContext dbContext)
    {
        // Just add shard block to database
        ShardBlock shardBlock = new()
        {
            Shard = shard.Shard,
            Seqno = shard.Seqno,
            RootHash = shard.RootHash,
            FileHash = shard.FileHash
        };
        await dbContext.ShardBlocks.AddAsync(shardBlock);
    }
}