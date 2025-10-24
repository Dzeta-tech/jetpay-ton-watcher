using JetPay.TonWatcher.Domain.Entities;

namespace JetPay.TonWatcher.Application.Interfaces;

public interface IShardBlockRepository
{
    Task<ShardBlock?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ShardBlock>> GetUnprocessedAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task<long> GetMaxSeqnoAsync(long shard, CancellationToken cancellationToken = default);
    Task AddAsync(ShardBlock shardBlock, CancellationToken cancellationToken = default);
    Task UpdateAsync(ShardBlock shardBlock, CancellationToken cancellationToken = default);
}