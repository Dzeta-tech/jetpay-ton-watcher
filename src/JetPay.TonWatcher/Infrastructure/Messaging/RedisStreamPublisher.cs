using System.Text.Json;
using JetPay.TonWatcher.Application.Interfaces;
using StackExchange.Redis;

namespace JetPay.TonWatcher.Infrastructure.Messaging;

public class RedisStreamPublisher(RedisDatabase redis, ILogger<RedisStreamPublisher> logger) : IMessagePublisher
{
    const string StreamKey = "ton-transactions";

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            string json = JsonSerializer.Serialize(message);
            NameValueEntry[] entries = [new NameValueEntry("data", json)];

            await redis.StreamAddAsync(StreamKey, entries);

            logger.LogDebug("Published message to Redis stream: {MessageType}", typeof(T).Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error publishing message to Redis stream");
        }
    }
}

