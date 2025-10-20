using BloomFilter;
using JetPay.TonWatcher.Data;
using JetPay.TonWatcher.Data.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using TonSdk.Adnl.LiteClient;
using TonSdk.Core;

namespace JetPay.TonWatcher.Services;

public class BlockProcessor(
    ILogger<BlockProcessor> logger,
    LiteClientProvider liteClientProvider,
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
        try
        {
            logger.LogInformation("Processing shard {Shard} {Seqno}", shard.Shard, shard.Seqno);

            // Get shard transactions
            BlockIdExtended blockId = new(
                shard.Workchain,
                Convert.FromBase64String(shard.RootHash),
                Convert.FromBase64String(shard.FileHash),
                shard.Shard,
                (int)shard.Seqno
            );

            ListBlockTransactionsResult transactionsResult = await liteClientProvider.GetBlockTransactionsAsync(blockId);

            if (transactionsResult.InComplete)
            {
                logger.LogWarning("Block {Shard} {Seqno} transactions incomplete, skipping", shard.Shard, shard.Seqno);
                return;
            }

            if (transactionsResult.TransactionIds == null || transactionsResult.TransactionIds.Length == 0)
                return;

            foreach (var tx in transactionsResult.TransactionIds)
            {
                string accountAddress = new Address(shard.Workchain, tx.Account).ToString();
                if (!await addressBloomFilter.ContainsAsync(accountAddress))
                    continue;

                // Bloom filter might give false positives, so we need to check if the address is actually in the database and active
                TrackedAddress? trackedAddress = await dbContext.TrackedAddresses.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Address == accountAddress, stoppingToken);
                if (trackedAddress is null || !trackedAddress.IsTrackingActive)
                    continue;

                // Process transaction
                await ProcessTransaction(accountAddress, Convert.ToBase64String(tx.Hash), (ulong)tx.Lt, botClient, stoppingToken);
            }

            shard.MarkAsProcessed();
            logger.LogInformation("Shard {Shard} {Seqno} processed", shard.Shard, shard.Seqno);
        }
        catch (Exception ex) when (ex.Message.Contains("LITE_SERVER_NOTREADY"))
        {
            logger.LogWarning(ex, "Lite server not ready, skipping shard {Shard} {Seqno}", shard.Shard, shard.Seqno);
            return;
        }
    }

    async Task ProcessTransaction(string account, string txHash, ulong lt, TelegramBotClient botClient,
        CancellationToken stoppingToken)
    {
        // TODO: Implement transaction processing
        logger.LogWarning("Found transaction {Account}. TxHash {Hash} Lt {Lt}", account, txHash, lt);
        await botClient.SendMessage(731818836, $"Found transaction {txHash} (Lt: {lt})",
            cancellationToken: stoppingToken);
    }
}