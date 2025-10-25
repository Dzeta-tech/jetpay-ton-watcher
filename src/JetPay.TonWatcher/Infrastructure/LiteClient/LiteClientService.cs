using System.Threading.RateLimiting;
using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Configuration;
using TonSdk.Adnl.LiteClient;
using TonSdk.Core.Boc;
using BlockIdExtended = TonSdk.Adnl.LiteClient.BlockIdExtended;

namespace JetPay.TonWatcher.Infrastructure.LiteClient;

public class LiteClientService : ILiteClientService, IAsyncDisposable
{
    readonly LiteClientV2 client;
    readonly ILogger<LiteClientService> logger;
    readonly TokenBucketRateLimiter rateLimiter;

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

        // Create client with single engine - auto-connects on first query, thread-safe
        client = LiteClientV2.CreateSingle(
            config.LiteClient.Host,
            config.LiteClient.Port,
            config.LiteClient.PublicKey,
            reconnectTimeoutMs: 5000);

        // Subscribe to engine events for logging
        var engine = client.Engine;
        engine.Connected += () => logger.LogInformation("LiteClient connected");
        engine.Ready += () => logger.LogInformation("LiteClient ready");
        engine.Closed += () => logger.LogWarning("LiteClient connection closed, will auto-reconnect");
        engine.Error += (ex) => logger.LogError(ex, "LiteClient error occurred");

        logger.LogInformation("LiteClient configured for {Host}:{Port} with {RPS} RPS (auto-connecting, thread-safe, event-driven)",
            config.LiteClient.Host, config.LiteClient.Port, config.LiteClient.Ratelimit);
    }

    public async ValueTask DisposeAsync()
    {
        client.Dispose();
        await rateLimiter.DisposeAsync();
    }

    public Task InitializeAsync()
    {
        // No-op: new client auto-connects on first query
        logger.LogInformation("LiteClient will auto-connect on first query");
        return Task.CompletedTask;
    }

    public async Task<MasterChainInfoExtended> GetMasterChainInfoAsync()
    {
        return await ExecuteAsync(async () => await client.GetMasterChainInfoExtendedAsync());
    }

    public async Task<BlockIdExtended[]> GetShardsAsync(BlockIdExtended blockId)
    {
        byte[] shardsData = await ExecuteAsync(async () => await client.GetAllShardsInfoAsync(blockId));
        return DeserializeShardsInformationResult(shardsData);
    }

    public async Task<BlockIdExtended?> LookupBlockAsync(int workchain, long shard, long seqno)
    {
        BlockHeader? blockHeader = await ExecuteAsync(async () => await client.LookupBlockAsync(workchain, shard, seqno));
        return blockHeader?.BlockId;
    }

    public async Task<ListBlockTransactionsResult> GetBlockTransactionsAsync(BlockIdExtended blockId,
        uint count = 10000)
    {
        return await ExecuteAsync(async () => await client.ListBlockTransactionsAsync(blockId, count));
    }

    async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        using RateLimitLease lease = await rateLimiter.AcquireAsync(permitCount: 1);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Failed to acquire rate limit token - queue full");

        // Client + engine handle all connection/reconnection logic internally
        // Engine is thread-safe, just execute the query
        return await operation();
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
