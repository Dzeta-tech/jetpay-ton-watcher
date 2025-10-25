using System.Threading.RateLimiting;
using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Configuration;
using TonSdk.Adnl.LiteClient;
using TonSdk.Core.Boc;
using BlockIdExtended = TonSdk.Adnl.LiteClient.BlockIdExtended;

namespace JetPay.TonWatcher.Infrastructure.LiteClient;

public class LiteClientService : ILiteClientService, IAsyncDisposable
{
    readonly TonSdk.Adnl.LiteClient.LiteClient client;
    readonly ILogger<LiteClientService> logger;
    readonly TokenBucketRateLimiter rateLimiter;
    readonly SemaphoreSlim connectionLock = new(1, 1);
    bool isInitialized;

    public LiteClientService(AppConfiguration config, ILogger<LiteClientService> logger)
    {
        this.logger = logger;

        rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = true,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokenLimit = config.LiteClient.Ratelimit,
            TokensPerPeriod = config.LiteClient.Ratelimit,
            QueueLimit = 10000
        });

        client = new TonSdk.Adnl.LiteClient.LiteClient(config.LiteClient.Host, config.LiteClient.Port,
            config.LiteClient.PublicKey);

        logger.LogInformation("LiteClient configured for {Host}:{Port} with {RPS} RPS (thread-safe SDK, no distributed lock needed)",
            config.LiteClient.Host, config.LiteClient.Port, config.LiteClient.Ratelimit);
    }

    public async ValueTask DisposeAsync()
    {
        client.Disconnect();
        await rateLimiter.DisposeAsync();
        connectionLock.Dispose();
    }

    public async Task InitializeAsync()
    {
        await EnsureConnectedAsync();
    }

    async Task EnsureConnectedAsync()
    {
        // Fast path: already connected
        if (isInitialized)
            return;

        // Slow path: need to acquire lock and potentially connect
        await connectionLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (isInitialized)
                return;

            logger.LogInformation("Connecting to LiteClient...");
            await client.Connect();
            isInitialized = true;
            logger.LogInformation("LiteClient connected successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to LiteClient");
            isInitialized = false;
            throw;
        }
        finally
        {
            connectionLock.Release();
        }
    }

    public async Task<MasterChainInfoExtended> GetMasterChainInfoAsync()
    {
        return await ExecuteAsync(liteClient => liteClient.GetMasterChainInfoExtended());
    }

    public async Task<BlockIdExtended[]> GetShardsAsync(BlockIdExtended blockId)
    {
        byte[] shardsData = await ExecuteAsync(liteClient => liteClient.GetAllShardsInfo(blockId));
        return DeserializeShardsInformationResult(shardsData);
    }

    public async Task<BlockIdExtended?> LookupBlockAsync(int workchain, long shard, long seqno)
    {
        BlockHeader? blockHeader = await ExecuteAsync(liteClient => liteClient.LookUpBlock(workchain, shard, seqno));
        return blockHeader?.BlockId;
    }

    public async Task<ListBlockTransactionsResult> GetBlockTransactionsAsync(BlockIdExtended blockId,
        uint count = 10000)
    {
        return await ExecuteAsync(liteClient => liteClient.ListBlockTransactions(blockId, count));
    }

    async Task<T> ExecuteAsync<T>(Func<TonSdk.Adnl.LiteClient.LiteClient, Task<T>> operation)
    {
        using RateLimitLease lease = await rateLimiter.AcquireAsync(permitCount: 1);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Failed to acquire rate limit token - queue full");

        // Try to ensure connected without blocking on lock
        if (!isInitialized)
        {
            // Fast path: someone else might be connecting, wait briefly
            await Task.Delay(50);
            
            // If still not initialized, trigger connection
            if (!isInitialized && connectionLock.Wait(0)) // Try acquire without waiting
            {
                try
                {
                    if (!isInitialized) // Double-check
                    {
                        logger.LogInformation("Connecting to LiteClient...");
                        await client.Connect();
                        isInitialized = true;
                        logger.LogInformation("LiteClient connected successfully");
                    }
                }
                finally
                {
                    connectionLock.Release();
                }
            }
        }

        try
        {
            // SDK is thread-safe with proper locking on cipher state
            return await operation(client);
        }
        catch (Exception ex) when (
            ex.Message.Contains("Connection to lite server must be init") ||
            ex is IndexOutOfRangeException) // Catch cipher state corruption
        {
            // Connection issue detected - trigger reconnection in background
            // Don't wait for it, just fail this operation and let caller retry
            if (connectionLock.Wait(0)) // Non-blocking attempt to reconnect
            {
                try
                {
                    logger.LogWarning(ex, "LiteClient connection issue detected, reconnecting in background...");
                    client.Disconnect();
                    isInitialized = false;
                    
                    // Fire-and-forget reconnection
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(100); // Brief delay for cleanup
                        await connectionLock.WaitAsync();
                        try
                        {
                            if (!isInitialized) // Check if someone else already reconnected
                            {
                                await client.Connect();
                                isInitialized = true;
                                logger.LogInformation("LiteClient reconnected successfully");
                            }
                        }
                        catch (Exception reconnectEx)
                        {
                            logger.LogError(reconnectEx, "Failed to reconnect LiteClient");
                        }
                        finally
                        {
                            connectionLock.Release();
                        }
                    });
                }
                finally
                {
                    connectionLock.Release();
                }
            }
            
            // Throw the error - caller (MasterchainSyncService) will handle retry
            throw;
        }
    }

    BlockIdExtended[] DeserializeShardsInformationResult(byte[] data)
    {
        Cell[]? cells = BagOfCells.DeserializeBoc(new Bits(data));
        List<BlockIdExtended> shards = [];

        foreach (Cell cell in cells)
        {
            HashmapOptions<uint, CellSlice> hmOptions = new()
            {
                KeySize = 32,
                Serializers = new HashmapSerializers<uint, CellSlice>
                {
                    Key = k => new BitsBuilder(32).StoreUInt(k, 32).Build(),
                    Value = v => new CellBuilder().Build()
                },
                Deserializers = new HashmapDeserializers<uint, CellSlice>
                {
                    Key = k => (uint)k.Parse().LoadUInt(32),
                    Value = v => v.Parse()
                }
            };

            HashmapE<uint, CellSlice>? hashes = cell.Parse().LoadDict(hmOptions);
            CellSlice? binTree = hashes.Get(0).LoadRef().Parse();
            shards = [];
            LoadBinTreeR(binTree, ref shards);
        }

        return shards.ToArray();
    }

    static BlockIdExtended LoadShardDescription(CellSlice slice)
    {
        uint type = (uint)slice.LoadUInt(4);

        if (type != 0xa && type != 0xb)
            throw new Exception("not a ShardDescr");

        int seqno = (int)slice.LoadUInt(32);
        slice.LoadUInt(32);
        slice.LoadUInt(64);
        slice.LoadUInt(64);
        byte[] rootHash = slice.LoadBits(256).ToBytes();
        byte[] fileHash = slice.LoadBits(256).ToBytes();
        slice.LoadBit();
        slice.LoadBit();
        slice.LoadBit();
        slice.LoadBit();
        slice.LoadBit();
        slice.LoadUInt(3);
        slice.LoadUInt(32);
        long shard = (long)slice.LoadInt(64);

        return new BlockIdExtended(0, rootHash, fileHash, shard, seqno);
    }

    static void LoadBinTreeR(CellSlice slice, ref List<BlockIdExtended> shards)
    {
        if (!slice.LoadBit())
        {
            shards.Add(LoadShardDescription(slice));
        }
        else
        {
            LoadBinTreeR(slice.LoadRef().Parse(), ref shards);
            LoadBinTreeR(slice.LoadRef().Parse(), ref shards);
        }
    }
}
