using MediatR;

namespace JetPay.TonWatcher.Application.Commands.SyncShardBlocks;

public record SyncShardBlocksCommand : IRequest<SyncShardBlocksResult>;

public record SyncShardBlocksResult
{
    public bool Success { get; init; }
    public int BlocksAdded { get; init; }
}