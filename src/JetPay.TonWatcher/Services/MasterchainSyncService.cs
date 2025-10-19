using JetPay.TonWatcher.Data;
using JetPay.TonWatcher.Data.Models;
using Microsoft.EntityFrameworkCore;
using TonSdk.Client;

namespace JetPay.TonWatcher.Services;

public class MasterchainSyncService(
    ILogger<MasterchainSyncService> logger,
    ITonClientFactory tonClientFactory,
    ApplicationDbContext dbContext) : BackgroundService
{
    readonly TimeSpan syncInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ITonClient client = tonClientFactory.GetClient();

            // if no blocks in db, latest seqno is 0
            long lastSeqnoInDb = await dbContext.MasterchainBlocks.OrderByDescending(x => x.Seqno).Select(x => x.Seqno)
                .FirstOrDefaultAsync(stoppingToken);

            logger.LogInformation("Latest seqno in db: {LastSeqno}", lastSeqnoInDb);

            MasterchainInformationResult? masterchainInfo = await client.GetMasterchainInfo();
            if (!masterchainInfo.HasValue)
                goto Delay;

            long currentLatestSeqno = masterchainInfo.Value.LastBlock.Seqno;

            // If no blocks in db, set last seqno to current latest seqno - 1 to process the latest block
            if (lastSeqnoInDb == 0)
                lastSeqnoInDb = currentLatestSeqno - 1;

            logger.LogInformation("Processing blocks from {LastSeqno} to {CurrentLatestSeqno}", lastSeqnoInDb,
                currentLatestSeqno);

            // From latest saved seqno + 1 to current latest seqno, process each block
            for (long seqno = lastSeqnoInDb + 1; seqno <= currentLatestSeqno; seqno++)
                await ProcessBlock(seqno);

            await dbContext.SaveChangesAsync(stoppingToken);

            Delay:
            await Task.Delay(syncInterval, stoppingToken);
        }
    }

    async Task ProcessBlock(long seqno)
    {
        MasterchainBlock block = new()
        {
            Seqno = seqno
        };

        await dbContext.MasterchainBlocks.AddAsync(block);
    }
}