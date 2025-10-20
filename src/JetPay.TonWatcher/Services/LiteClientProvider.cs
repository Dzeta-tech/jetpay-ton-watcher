using System.Threading.RateLimiting;
using JetPay.TonWatcher.Configuration;
using TonSdk.Adnl.LiteClient;
using TonSdk.Client;
using TonSdk.Core.Boc;
using BlockIdExtended = TonSdk.Adnl.LiteClient.BlockIdExtended;

namespace JetPay.TonWatcher.Services;

public class LiteClientProvider : IAsyncDisposable
{
    readonly LiteClient client;
    readonly TokenBucketRateLimiter rateLimiter;
    readonly ILogger<LiteClientProvider> logger;

    public LiteClientProvider(AppConfiguration config, ILogger<LiteClientProvider> logger)
    {
        this.logger = logger;
        
        rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = true,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokenLimit = config.LiteClient.Ratelimit,
            TokensPerPeriod = config.LiteClient.Ratelimit
        });

        client = new LiteClient(config.LiteClient.Host, config.LiteClient.Port, config.LiteClient.PublicKey);
        
        logger.LogInformation("LiteClient configured for {Host}:{Port} with {RPS} RPS", 
            config.LiteClient.Host, config.LiteClient.Port, config.LiteClient.Ratelimit);
    }

    public async Task InitializeAsync()
    {
        logger.LogInformation("Connecting to LiteClient...");
        await client.Connect();
        logger.LogInformation("LiteClient connected successfully");
    }

    public async Task<T> ExecuteAsync<T>(Func<LiteClient, Task<T>> operation)
    {
        using RateLimitLease lease = await rateLimiter.AcquireAsync(1);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Failed to acquire rate limit token");
        
        return await operation(client);
    }

    public async Task<MasterChainInfoExtended> GetMasterChainInfoAsync()
    {
        return await ExecuteAsync(client => client.GetMasterChainInfoExtended());
    }

    public async Task<BlockIdExtended[]> GetShardsAsync(BlockIdExtended blockId)
    {
        byte[] shardsData = await ExecuteAsync(client => client.GetAllShardsInfo(blockId));
        return DeserializeShardsInformationResult(shardsData);
    }

    public async Task<BlockIdExtended?> LookupBlockAsync(int workchain, long shard, long seqno)
    {
        BlockHeader? blockHeader = await ExecuteAsync(client => client.LookUpBlock(workchain, shard, seqno));

        return blockHeader?.BlockId;
    }

    public async Task<ListBlockTransactionsResult> GetBlockTransactionsAsync(BlockIdExtended blockId, uint count = 10000)
    {
        return await ExecuteAsync(client => client.ListBlockTransactions(blockId, count));
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
        byte[] rootHash = slice.LoadBits(256).ToBytes(); // root_hash
        byte[] fileHash = slice.LoadBits(256).ToBytes(); // file_hash
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

    public async ValueTask DisposeAsync()
    {
        await rateLimiter.DisposeAsync();
    }
}

