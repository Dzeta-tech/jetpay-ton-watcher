using Ton.Core.Addresses;

namespace JetPay.TonWatcher.Domain.ValueObjects;

public record TransactionInfo
{
    public required Address Address { get; init; }
    public required string TxHash { get; init; }
    public required long LogicalTime { get; init; }
}