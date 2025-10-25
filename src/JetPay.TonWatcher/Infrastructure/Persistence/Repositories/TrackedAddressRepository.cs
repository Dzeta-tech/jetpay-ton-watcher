using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using TonSdk.Core;

namespace JetPay.TonWatcher.Infrastructure.Persistence.Repositories;

public class TrackedAddressRepository(ApplicationDbContext dbContext) : ITrackedAddressRepository
{
    public async Task<TrackedAddress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.TrackedAddresses
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<TrackedAddress?> GetByAccountAsync(int workchain, byte[] account,
        CancellationToken cancellationToken = default)
    {
        Address searchAddress = new(workchain, account);
        return await dbContext.TrackedAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Address == searchAddress, cancellationToken);
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
}