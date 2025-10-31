using JetPay.TonWatcher.Domain.Entities;
using JetPay.TonWatcher.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ton.Core.Addresses;

namespace JetPay.TonWatcher.Application.Commands;

public record DisableTrackedAddressCommand : IRequest<bool>
{
    public required Address Address { get; init; }
}

public class DisableTrackedAddressCommandHandler(
    ApplicationDbContext dbContext)
    : IRequestHandler<DisableTrackedAddressCommand, bool>
{
    public async Task<bool> Handle(DisableTrackedAddressCommand request, CancellationToken cancellationToken)
    {
        TrackedAddress? trackedAddress = await dbContext.TrackedAddresses
            .FirstOrDefaultAsync(x => x.Address == request.Address, cancellationToken);

        if (trackedAddress is not { IsTrackingActive: true }) return false;

        trackedAddress.Deactivate();

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}