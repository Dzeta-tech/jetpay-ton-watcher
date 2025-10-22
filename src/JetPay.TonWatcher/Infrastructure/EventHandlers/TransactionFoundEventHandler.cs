using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Domain.Events;
using MediatR;

namespace JetPay.TonWatcher.Infrastructure.EventHandlers;

public class TransactionFoundEventHandler(
    IMessagePublisher messagePublisher,
    ILogger<TransactionFoundEventHandler> logger)
    : INotificationHandler<TransactionFoundEvent>
{
    public async Task Handle(TransactionFoundEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling TransactionFoundEvent for address {Address}", notification.Address);

        await messagePublisher.PublishAsync(notification, cancellationToken);

        logger.LogInformation("Published transaction event to RabbitMQ for {Address}", notification.Address);
    }
}

