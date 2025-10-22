using JetPay.TonWatcher.Domain.ValueObjects;
using MediatR;

namespace JetPay.TonWatcher.Application.Commands.ProcessShardBlock;

public record ProcessShardBlockCommand : IRequest<ProcessShardBlockResult>
{
    public required Guid ShardBlockId { get; init; }
}

public record ProcessShardBlockResult
{
    public bool Success { get; init; }
    public int TransactionsFound { get; init; }
    public List<TransactionInfo> Transactions { get; init; } = new();
}

