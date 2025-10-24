using JetPay.TonWatcher.Domain.Entities;

namespace JetPay.TonWatcher.Application.Interfaces;

public interface ITrackedAddressRepository
{
    Task<TrackedAddress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TrackedAddress?> GetByAccountAsync(int workchain, byte[] account,
        CancellationToken cancellationToken = default);

    Task<List<TrackedAddress>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TrackedAddress trackedAddress, CancellationToken cancellationToken = default);
    Task UpdateAsync(TrackedAddress trackedAddress, CancellationToken cancellationToken = default);
}