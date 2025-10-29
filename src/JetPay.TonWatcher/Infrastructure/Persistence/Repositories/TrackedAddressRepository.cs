using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JetPay.TonWatcher.Infrastructure.Persistence.Repositories;

public class TrackedAddressRepository(ApplicationDbContext dbContext) : ITrackedAddressRepository
{
    public async Task<TrackedAddress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.TrackedAddresses
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<TrackedAddress>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.TrackedAddresses
            .Where(x => x.IsTrackingActive)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TrackedAddress trackedAddress, CancellationToken cancellationToken = default)
    {
        await dbContext.TrackedAddresses.AddAsync(trackedAddress, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TrackedAddress trackedAddress, CancellationToken cancellationToken = default)
    {
        dbContext.TrackedAddresses.Update(trackedAddress);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TrackedAddress?> GetByAddressHashAsync(byte[] addressHash,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TrackedAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Hash == addressHash, cancellationToken);
    }
}