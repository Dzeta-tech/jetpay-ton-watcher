using JetPay.TonWatcher.Application.Interfaces;
using StackExchange.Redis;

namespace JetPay.TonWatcher.Infrastructure.Services;

public class RedisDistributedLock(IConnectionMultiplexer redis, ILogger<RedisDistributedLock> logger) : IDistributedLock
{
    readonly IDatabase _db = redis.GetDatabase();

    public async Task<IDisposable?> AcquireLockAsync(string resource, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        string lockKey = $"lock:{resource}";
        string queueKey = $"lock:queue:{resource}";
        string requestId = Guid.NewGuid().ToString();
        
        // Add ourselves to the queue (right push = FIFO)
        await _db.ListRightPushAsync(queueKey, requestId);

        try
        {
            DateTime startTime = DateTime.UtcNow;
            
            while (DateTime.UtcNow - startTime < timeout)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    await RemoveFromQueue(queueKey, requestId);
                    return null;
                }

                // Check if we're at the front of the queue
                RedisValue frontOfQueue = await _db.ListGetByIndexAsync(queueKey, 0);
                
                if (frontOfQueue != requestId)
                {
                    // Not our turn yet - wait a bit
                    await Task.Delay(5, cancellationToken);
                    continue;
                }

                // We're at the front - try to acquire the actual lock
                // Lock expires in case holder crashes
                TimeSpan lockExpiry = TimeSpan.FromMilliseconds(500); // Short expiry for lite client calls
                bool acquired = await _db.StringSetAsync(lockKey, requestId, lockExpiry, When.NotExists);
                
                if (acquired)
                {
                    // Remove ourselves from queue
                    await _db.ListLeftPopAsync(queueKey);
                    return new RedisLock(_db, lockKey, queueKey, requestId);
                }

                // Lock exists but we're at front of queue - previous holder might have crashed
                // Check if lock is expired by trying to acquire again
                await Task.Delay(5, cancellationToken);
            }

            // Timeout - remove from queue
            await RemoveFromQueue(queueKey, requestId);
            logger.LogWarning("Failed to acquire lock for {Resource} within {Timeout}ms, queue depth was {Depth}", 
                resource, timeout.TotalMilliseconds, await _db.ListLengthAsync(queueKey));
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error acquiring lock for {Resource}", resource);
            await RemoveFromQueue(queueKey, requestId);
            return null;
        }
    }

    async Task RemoveFromQueue(string queueKey, string requestId)
    {
        try
        {
            // Remove our request from queue (count: 0 = remove all occurrences)
            await _db.ListRemoveAsync(queueKey, requestId, 0);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    class RedisLock(IDatabase db, string lockKey, string queueKey, string requestId) : IDisposable
    {
        bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                // Release the lock only if we still own it
                string script = @"
                    if redis.call('get', KEYS[1]) == ARGV[1] then
                        return redis.call('del', KEYS[1])
                    else
                        return 0
                    end";

                db.ScriptEvaluate(script, new RedisKey[] { lockKey }, new RedisValue[] { requestId });
                
                // Clean up old queue entries (keep queue from growing unbounded)
                // This is a safety measure in case queue cleanup failed somewhere
                long queueLength = db.ListLength(queueKey);
                if (queueLength > 100)
                {
                    // Trim to keep last 50 entries
                    db.ListTrim(queueKey, -50, -1);
                }
            }
            catch (Exception)
            {
                // Ignore release errors - lock will expire anyway
            }
        }
    }
}
