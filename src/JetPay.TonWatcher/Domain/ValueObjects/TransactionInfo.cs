namespace JetPay.TonWatcher.Domain.ValueObjects;

public record TransactionInfo
{
    public required string Address { get; init; }
    public required string TxHash { get; init; }
    public required long LogicalTime { get; init; }
    public required byte[] AccountHash { get; init; }
    public required int Workchain { get; init; }
}