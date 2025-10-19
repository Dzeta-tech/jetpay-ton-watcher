using BloomFilter;
using JetPay.TonWatcher.Data;
using JetPay.TonWatcher.Data.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using TonSdk.Client;

namespace JetPay.TonWatcher.Services;

public class BlockProcessor(
    ILogger<BlockProcessor> logger,
    ITonClientFactory tonClientFactory,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    readonly TimeSpan syncInterval = TimeSpan.FromSeconds(1); // Often check for new blocks

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            IBloomFilter addressBloomFilter = scope.ServiceProvider.GetRequiredService<IBloomFilter>();
            TelegramBotClient botClient = scope.ServiceProvider.GetRequiredService<TelegramBotClient>();

            // Check for any unprocessed blocks in db, check from oldest to newest
            ShardBlock[] unprocessedBlocks = await dbContext.ShardBlocks.Where(x => !x.IsProcessed)
                .OrderBy(x => x.Shard).ThenBy(x => x.Seqno)
                .ToArrayAsync(stoppingToken);
            foreach (ShardBlock block in unprocessedBlocks)
                await ProcessShard(block, dbContext, addressBloomFilter, botClient, stoppingToken);

            if (unprocessedBlocks.Length > 0)
                await dbContext.SaveChangesAsync(stoppingToken);
            else
                await Task.Delay(syncInterval, stoppingToken);
        }
    }

    async Task ProcessShard(ShardBlock shard, ApplicationDbContext dbContext, IBloomFilter addressBloomFilter,
        TelegramBotClient botClient, CancellationToken stoppingToken)
    {
        logger.LogInformation("Processing shard {Shard} {Seqno}", shard.Shard, shard.Seqno);

        // Get shard transactions
        TonClient client = tonClientFactory.GetClient();
        BlockTransactionsResult? transactions =
            await client.GetBlockTransactions(shard.Workchain, shard.Shard, shard.Seqno, shard.RootHash, shard.FileHash,
                count: 10000); // TODO: Idk how many transactions to get

        if (!transactions.HasValue)
            return;

        foreach (ShortTransactionsResult transaction in transactions.Value.Transactions)
        {
            if (!await addressBloomFilter.ContainsAsync(transaction.Account))
                continue;

            // Bloom filter might give false positives, so we need to check if the address is actually in the database and active
            TrackedAddress? trackedAddress = await dbContext.TrackedAddresses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Address == transaction.Account, stoppingToken);
            if (trackedAddress is null || !trackedAddress.IsTrackingActive)
                continue;

            // Process transaction
            await ProcessTransaction(transaction, botClient, stoppingToken);
        }

        shard.MarkAsProcessed();
        logger.LogInformation("Shard {Shard} {Seqno} processed", shard.Shard, shard.Seqno);
    }

    async Task ProcessTransaction(ShortTransactionsResult transaction, TelegramBotClient botClient,
        CancellationToken stoppingToken)
    {
        // TODO: Implement transaction processing
        logger.LogWarning("Found transaction {Account}. TxHash {Hash}", transaction.Account, transaction.Hash);
        await botClient.SendMessage(731818836, $"Found transaction {transaction.Hash}",
            cancellationToken: stoppingToken);
    }
}