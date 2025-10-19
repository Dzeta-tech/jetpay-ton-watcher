using System.Threading.RateLimiting;
using TonSdk.Client;
using Microsoft.AspNetCore.RateLimiting;

public class RateLimitedTonClient 
{
    // Allow 9 requests per second
    readonly FixedWindowRateLimiter limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions()
    {
        PermitLimit = 9,
        Window = TimeSpan.FromSeconds(1)
    });
    
    readonly TonClient client;
    
    public RateLimitedTonClient(TonClient client)
    {
        this.client = client;
    }
    
    public async Task<MasterchainInformationResult?> GetMasterchainInfo()
    {
        await limiter.AcquireAsync(1);
        return await client.GetMasterchainInfo();
    }

    public async Task<ShardsInformationResult?> Shards(long seqno)
    {
        await limiter.AcquireAsync(1);
        return await client.Shards(seqno);
    }

    public async Task<BlockIdExtended?> LookUpBlock(int workchain, long shard, long seqno)
    {
        await limiter.AcquireAsync(1);
        return await client.LookUpBlock(workchain, shard, seqno);
    }

    public async Task<BlockTransactionsResult?> GetBlockTransactions(int workchain, long shard, long seqno, string rootHash, string fileHash, ulong? count)
    {
        await limiter.AcquireAsync(1);
        return await client.GetBlockTransactions(workchain, shard, seqno, rootHash, fileHash, count);
    }
}