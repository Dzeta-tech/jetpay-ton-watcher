using System.Text;
using System.Text.Json;
using JetPay.TonWatcher.Application.Interfaces;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Net;

namespace JetPay.TonWatcher.Infrastructure.Messaging;

public class NatsJetStreamPublisher(INatsConnection natsConnection) : IMessagePublisher
{
    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        INatsJSContext jetStream = natsConnection.CreateJetStreamContext();
        string json = JsonSerializer.Serialize(message);
        byte[] data = Encoding.UTF8.GetBytes(json);
        string subject = $"ton.transactions.{typeof(T).Name.ToLowerInvariant()}";
        await jetStream.PublishAsync(subject, data, cancellationToken: cancellationToken);
    }
}