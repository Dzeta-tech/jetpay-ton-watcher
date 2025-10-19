using System.Threading.RateLimiting;
using TonSdk.Client;
using Microsoft.AspNetCore.RateLimiting;

public class RateLimitedTonClient 
{
    // Allow 9 requests per second
    readonly TokenBucketRateLimiter limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions()
    {
        AutoReplenishment = true,
        ReplenishmentPeriod = TimeSpan.FromSeconds(1),
        TokenLimit = 10,
        TokensPerPeriod = 9
    });
    
    readonly TonClient client;
    
    public RateLimitedTonClient(TonClient client)
    {
        this.client = client;
    }
    
    public async Task<MasterchainInformationResult?> GetMasterchainInfo()
    {
        using var lease = await limiter.AcquireAsync(1);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Failed to acquire rate limit token");
        
        Console.WriteLine("Getting masterchain info");
        return await client.GetMasterchainInfo();
    }

    public async Task<ShardsInformationResult?> Shards(long seqno)
    {
        using var lease = await limiter.AcquireAsync(1);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Failed to acquire rate limit token");
        
        Console.WriteLine("Getting shards");
        return await client.Shards(seqno);
    }

    public async Task<BlockIdExtended?> LookUpBlock(int workchain, long shard, long seqno)
    {
        using var lease = await limiter.AcquireAsync(1);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Failed to acquire rate limit token");
        
        Console.WriteLine("Looking up block");
        return await client.LookUpBlock(workchain, shard, seqno);
    }

    public async Task<BlockTransactionsResult?> GetBlockTransactions(int workchain, long shard, long seqno, string rootHash, string fileHash, ulong? count)
    {
        using var lease = await limiter.AcquireAsync(1);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Failed to acquire rate limit token");
        
        Console.WriteLine("Getting block transactions");
        return await client.GetBlockTransactions(workchain, shard, seqno, rootHash, fileHash, count);
    }
}