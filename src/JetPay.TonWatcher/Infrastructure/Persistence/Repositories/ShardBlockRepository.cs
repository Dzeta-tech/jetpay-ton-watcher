using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JetPay.TonWatcher.Infrastructure.Persistence.Repositories;

public class ShardBlockRepository(ApplicationDbContext dbContext) : IShardBlockRepository
{
    public async Task<ShardBlock?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.ShardBlocks
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<ShardBlock>> GetUnprocessedAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        return await dbContext.ShardBlocks
            .Where(x => !x.IsProcessed)
            .OrderBy(x => x.Shard)
            .ThenBy(x => x.Seqno)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetMaxSeqnoAsync(long shard, CancellationToken cancellationToken = default)
    {
        return await dbContext.ShardBlocks
            .AsNoTracking()
            .Where(x => x.Shard == shard)
            .OrderByDescending(x => x.Seqno)
            .Select(x => x.Seqno)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(ShardBlock shardBlock, CancellationToken cancellationToken = default)
    {
        await dbContext.ShardBlocks.AddAsync(shardBlock, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ShardBlock shardBlock, CancellationToken cancellationToken = default)
    {
        dbContext.ShardBlocks.Update(shardBlock);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

