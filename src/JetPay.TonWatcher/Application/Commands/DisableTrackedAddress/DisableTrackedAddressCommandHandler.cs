using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Domain.Entities;
using MediatR;
using TonSdk.Core;

namespace JetPay.TonWatcher.Application.Commands.DisableTrackedAddress;

public class DisableTrackedAddressCommandHandler(
    ITrackedAddressRepository trackedAddressRepository,
    ILogger<DisableTrackedAddressCommandHandler> logger)
    : IRequestHandler<DisableTrackedAddressCommand, bool>
{
    public async Task<bool> Handle(DisableTrackedAddressCommand request, CancellationToken cancellationToken)
    {
        try
        {
            TrackedAddress? trackedAddress = await trackedAddressRepository
                .GetByAddressAsync(request.Address, cancellationToken);

            if (trackedAddress == null)
            {
                logger.LogWarning("Attempted to disable non-existent address {Address}", request.Address.ToRaw());
                return false;
            }

            if (!trackedAddress.IsTrackingActive)
            {
                logger.LogDebug("Address {Address} already disabled", request.Address.ToRaw());
                return true;
            }

            trackedAddress.Deactivate();
            await trackedAddressRepository.UpdateAsync(trackedAddress, cancellationToken);

            // Note: Bloom filter doesn't support removal, but we check IsTrackingActive anyway
            logger.LogInformation("Disabled tracked address {Address} with ID {Id}",
                request.Address.ToRaw(), trackedAddress.Id);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error disabling tracked address {Address}", request.Address.ToRaw());
            return false;
        }
    }
}

