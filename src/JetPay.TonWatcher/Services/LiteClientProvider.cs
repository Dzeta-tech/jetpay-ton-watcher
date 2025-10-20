using System.Threading.RateLimiting;
using JetPay.TonWatcher.Configuration;
using TonSdk.Adnl.LiteClient;
using TonSdk.Client;
using TonSdk.Core;
using TonSdk.Core.Boc;
using AccountState = TonSdk.Client.AccountState;
using BlockIdExtended = TonSdk.Adnl.LiteClient.BlockIdExtended;
using TransactionId = TonSdk.Client.TransactionId;

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
        const int maxRetries = 3;
        const int timeoutSeconds = 5;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("Connecting to LiteClient (attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);
                
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(timeoutSeconds));
                await client.Connect().WaitAsync(cts.Token);
                
                logger.LogInformation("LiteClient connected successfully");
                return;
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning(ex, "LiteClient connection timeout on attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
                if (attempt == maxRetries)
                    throw new Exception($"Failed to connect to LiteClient after {maxRetries} attempts", ex);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex, "LiteClient connection timeout on attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
                if (attempt == maxRetries)
                    throw new Exception($"Failed to connect to LiteClient after {maxRetries} attempts", ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "LiteClient connection error on attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
                if (attempt == maxRetries)
                    throw;
            }
            
            if (attempt < maxRetries)
            {
                int delaySeconds = attempt * 2; // Exponential backoff: 2s, 4s
                logger.LogInformation("Retrying in {Delay} seconds...", delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }
    }

    public async Task<T> ExecuteAsync<T>(Func<LiteClient, Task<T>> operation)
    {
        using RateLimitLease lease = await rateLimiter.AcquireAsync(1);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Failed to acquire rate limit token");
        
        // Add timeout to prevent hanging on API calls
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
        try
        {
            return await operation(client).WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("LiteClient operation timed out after 30 seconds");
        }
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

    public async Task<Coins> GetAccountBalanceAsync(string address)
    {
        Address addr = new(address);
        var addressInfo = await GetAddressInformation(addr);
        return addressInfo.Balance;
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
    
    internal async Task<AddressInformationResult> GetAddressInformation(Address address, BlockIdExtended? block = null)
        {
            AddressInformationResult result = new();
            AccountStateResult? res = await ExecuteAsync(client => client.GetAccountState(address, block));
            
            byte[] accountStateBytes = res.State;
            if (accountStateBytes.Length == 0)
            {
                result.State = AccountState.Uninit;
                return result;
            }
            
            CellSlice slice = Cell.From(new Bits(accountStateBytes)).Parse();
            
            slice.LoadBit(); // tag
            slice.LoadAddress(); // skip address (not needed)

            slice.LoadVarUInt(7);
            slice.LoadVarUInt(7);
            slice.LoadVarUInt(7);

            slice.LoadUInt32LE();
            
            if (slice.LoadBit())
                slice.LoadCoins();

            result.LastTransactionId = new TransactionId()
            {
                Lt = (ulong)slice.LoadUInt(64),
            };
            result.Balance = slice.LoadCoins();
            
            HashmapOptions<int, int> hmOptions = new()
            {
                KeySize = 32,
                Serializers = null,
                Deserializers = null
            };

            slice.LoadDict(hmOptions);

            if (slice.LoadBit()) // active
            {
                result.State = AccountState.Active;
                if(slice.LoadBit())
                    slice.LoadUInt(5);
                
                if (slice.LoadBit())
                {
                    slice.LoadBit();
                    slice.LoadBit();
                }

                if (slice.LoadBit())
                    result.Code = slice.LoadRef();
                
                if (slice.LoadBit())
                    result.Data = slice.LoadRef();
                
                if (slice.LoadBit())
                    slice.LoadRef();
            }
            else if (slice.LoadBit()) // frozen
            {
                result.State = AccountState.Frozen;
                result.FrozenHash = slice.LoadBits(256).ToString("base64");
            }
            else result.State = AccountState.Uninit;
            
            return result;
        }

    public async ValueTask DisposeAsync()
    {
        await rateLimiter.DisposeAsync();
    }
}

