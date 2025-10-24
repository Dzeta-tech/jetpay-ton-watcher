using JetPay.TonWatcher.Application.Interfaces;
using StackExchange.Redis;

namespace JetPay.TonWatcher.Infrastructure.Services;

public class RedisDistributedLock(IConnectionMultiplexer redis, ILogger<RedisDistributedLock> logger) : IDistributedLock
{
    readonly IDatabase _db = redis.GetDatabase();

    public async Task<IDisposable?> AcquireLockAsync(string resource, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        string lockKey = $"lock:{resource}";
        string lockValue = Guid.NewGuid().ToString();
        TimeSpan expiry = TimeSpan.FromSeconds(5); // Lock expires after 5 seconds as safety
        
        DateTime startTime = DateTime.UtcNow;
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

            // Try to acquire lock
            bool acquired = await _db.StringSetAsync(lockKey, lockValue, expiry, When.NotExists);
            
            if (acquired)
            {
                logger.LogDebug("Lock acquired for resource {Resource}", resource);
                return new RedisLock(_db, lockKey, lockValue, logger);
            }

            // Wait a bit before retrying (exponential backoff would be better for high contention)
            await Task.Delay(10, cancellationToken);
        }

        logger.LogWarning("Failed to acquire lock for resource {Resource} within {Timeout}ms", 
            resource, timeout.TotalMilliseconds);
        return null;
    }

    class RedisLock(IDatabase db, string lockKey, string lockValue, ILogger logger) : IDisposable
    {
        bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                // Only delete if we still own the lock (compare value)
                string? script = @"
                    if redis.call('get', KEYS[1]) == ARGV[1] then
                        return redis.call('del', KEYS[1])
                    else
                        return 0
                    end";

                db.ScriptEvaluate(script, new RedisKey[] { lockKey }, new RedisValue[] { lockValue });
                logger.LogDebug("Lock released for {LockKey}", lockKey);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error releasing lock {LockKey}", lockKey);
            }
        }
    }
}

