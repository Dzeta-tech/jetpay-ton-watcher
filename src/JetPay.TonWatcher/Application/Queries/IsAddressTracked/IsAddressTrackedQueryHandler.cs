using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Domain.Entities;
using MediatR;

namespace JetPay.TonWatcher.Application.Queries.IsAddressTracked;

public class IsAddressTrackedQueryHandler(
    ITrackedAddressRepository trackedAddressRepository)
    : IRequestHandler<IsAddressTrackedQuery, bool>
{
    public async Task<bool> Handle(IsAddressTrackedQuery request, CancellationToken cancellationToken)
    {
        TrackedAddress? trackedAddress = await trackedAddressRepository
            .GetByAddressHashAsync(request.Address.Hash, cancellationToken);

        return trackedAddress is { IsTrackingActive: true };
    }
}