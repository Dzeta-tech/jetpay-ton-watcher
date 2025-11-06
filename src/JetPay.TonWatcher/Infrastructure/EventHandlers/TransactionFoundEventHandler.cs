using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Domain.Events;
using MediatR;

namespace JetPay.TonWatcher.Infrastructure.EventHandlers;

public class TransactionFoundEventHandler(IMessagePublisher messagePublisher)
    : INotificationHandler<TransactionFoundEvent>
{
    public async Task Handle(TransactionFoundEvent notification, CancellationToken cancellationToken)
    {
        await messagePublisher.PublishAsync(notification, cancellationToken);
    }
}