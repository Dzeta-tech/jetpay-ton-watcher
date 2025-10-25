using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Domain.Entities;
using MediatR;
using TonSdk.Core;

namespace JetPay.TonWatcher.Application.Queries.IsAddressTracked;

public class IsAddressTrackedQueryHandler(
    ITrackedAddressRepository trackedAddressRepository,
    ILogger<IsAddressTrackedQueryHandler> logger)
    : IRequestHandler<IsAddressTrackedQuery, bool>
{
    public async Task<bool> Handle(IsAddressTrackedQuery request, CancellationToken cancellationToken)
    {
        try
        {
            TrackedAddress? trackedAddress = await trackedAddressRepository
                .GetByAddressAsync(request.Address, cancellationToken);

            // Return true only if address exists AND is active
            return trackedAddress != null && trackedAddress.IsTrackingActive;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if address {Address} is tracked", request.Address.ToRaw());
            // On error, return false (not tracked)
            return false;
        }
    }
}

