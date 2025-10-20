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
            // Lookup the block to get the correct root hash and file hash
            BlockIdExtended? blockId = await liteClientProvider.LookupBlockAsync(shard.Workchain, shard.Shard, shard.Seqno);
            
            if (blockId == null)
            {
                logger.LogWarning("Block {Shard}:{Seqno} not found, will retry later", shard.Shard, shard.Seqno);
                return;
            }

            // Get all transactions from the block (with high limit to avoid incomplete results)
            ListBlockTransactionsResult transactionsResult = await liteClientProvider.GetBlockTransactionsAsync(blockId, 100000);

            if (transactionsResult.TransactionIds == null || transactionsResult.TransactionIds.Length == 0)
            {
                shard.MarkAsProcessed();
                return;
            }

            int trackedTransactionsFound = 0;
            foreach (var tx in transactionsResult.TransactionIds)
            {
                // Use bytes directly for bloom filter check
                if (!await addressBloomFilter.ContainsAsync(tx.Account))
                    continue;

                string addressStr = new Address(shard.Workchain, tx.Account).ToString();
                logger.LogInformation("Bloom filter matched address {Address} in block {Shard}:{Seqno}", addressStr, shard.Shard, shard.Seqno);

                // Bloom filter might give false positives, so we need to check if the address is actually in the database and active
                TrackedAddress? trackedAddress = await dbContext.TrackedAddresses.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Workchain == shard.Workchain && x.Account == tx.Account, stoppingToken);
                if (trackedAddress is null)
                {
                    logger.LogWarning("Address {Address} matched bloom filter but NOT in database", addressStr);
                    continue;
                }
                
                if (!trackedAddress.IsTrackingActive)
                {
                    logger.LogInformation("Address {Address} found but tracking is inactive", addressStr);
                    continue;
                }

                // Process transaction
                trackedTransactionsFound++;
                await ProcessTransaction(addressStr, Convert.ToBase64String(tx.Hash), (ulong)tx.Lt, botClient, stoppingToken);
            }

            shard.MarkAsProcessed();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing shard {Shard}:{Seqno}, marking as processed to avoid infinite loop", shard.Shard, shard.Seqno);
            shard.MarkAsProcessed();
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