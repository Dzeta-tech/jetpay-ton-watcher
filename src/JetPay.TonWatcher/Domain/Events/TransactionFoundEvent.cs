using MediatR;
using Ton.Core.Addresses;

namespace JetPay.TonWatcher.Domain.Events;

public record TransactionFoundEvent : INotification
{
    public required Address Address { get; init; }
    public required string TxHash { get; init; }
    public required long Lt { get; init; }
}