using MediatR;

namespace JetPay.TonWatcher.Domain.Events;

public record TransactionFoundEvent : INotification
{
    public required string Address { get; init; }
    public required string TxHash { get; init; }
    public required ulong Lt { get; init; }
    public required DateTime DetectedAt { get; init; }
}