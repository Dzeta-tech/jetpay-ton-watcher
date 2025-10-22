using System.Text;
using System.Text.Json;
using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Configuration;
using RabbitMQ.Client;

namespace JetPay.TonWatcher.Infrastructure.Messaging;

public class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    readonly IConnection? connection;
    readonly IChannel? channel;
    readonly ILogger<RabbitMqPublisher> logger;
    readonly string exchangeName;

    public RabbitMqPublisher(AppConfiguration config, ILogger<RabbitMqPublisher> logger)
    {
        this.logger = logger;
        exchangeName = config.RabbitMq.ExchangeName;

        try
        {
            if (!config.RabbitMq.Enabled)
            {
                logger.LogInformation("RabbitMQ is disabled");
                return;
            }

            ConnectionFactory factory = new()
            {
                HostName = config.RabbitMq.Host,
                Port = config.RabbitMq.Port,
                UserName = config.RabbitMq.UserName,
                Password = config.RabbitMq.Password,
                VirtualHost = config.RabbitMq.VirtualHost
            };

            connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

            channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Fanout, durable: true).GetAwaiter().GetResult();

            logger.LogInformation("RabbitMQ publisher initialized for exchange {Exchange}", exchangeName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize RabbitMQ publisher");
        }
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        if (channel == null)
        {
            logger.LogWarning("RabbitMQ channel is not initialized, skipping message publish");
            return;
        }

        try
        {
            string json = JsonSerializer.Serialize(message);
            byte[] body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: string.Empty,
                body: body,
                cancellationToken: cancellationToken);

            logger.LogDebug("Published message to RabbitMQ: {MessageType}", typeof(T).Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error publishing message to RabbitMQ");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (channel != null)
            await channel.CloseAsync();
        
        if (connection != null)
            await connection.CloseAsync();
    }
}

