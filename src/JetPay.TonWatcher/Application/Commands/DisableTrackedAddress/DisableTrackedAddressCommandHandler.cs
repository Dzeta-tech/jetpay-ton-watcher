using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Domain.Entities;
using MediatR;

namespace JetPay.TonWatcher.Application.Commands.DisableTrackedAddress;

public class DisableTrackedAddressCommandHandler(
    ITrackedAddressRepository trackedAddressRepository,
    ILogger<DisableTrackedAddressCommandHandler> logger)
    : IRequestHandler<DisableTrackedAddressCommand, bool>
{
    public async Task<bool> Handle(DisableTrackedAddressCommand request, CancellationToken cancellationToken)
    {
        TrackedAddress? trackedAddress = await trackedAddressRepository
            .GetByAddressHashAsync(request.Address.Hash, cancellationToken);

        if (trackedAddress is not { IsTrackingActive: true }) return false;

        trackedAddress.Deactivate();
        await trackedAddressRepository.UpdateAsync(trackedAddress, cancellationToken);

        logger.LogInformation("Disabled tracked address {Address} with ID {Id}",
            request.Address.ToString(), trackedAddress.Id);

        return true;
    }
}