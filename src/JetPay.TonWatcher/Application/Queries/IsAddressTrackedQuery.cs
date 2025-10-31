using JetPay.TonWatcher.Domain.Entities;
using JetPay.TonWatcher.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ton.Core.Addresses;

namespace JetPay.TonWatcher.Application.Queries;

public record IsAddressTrackedQuery : IRequest<bool>
{
    public required Address Address { get; init; }
}

public class IsAddressTrackedQueryHandler(
    ApplicationDbContext dbContext)
    : IRequestHandler<IsAddressTrackedQuery, bool>
{
    public async Task<bool> Handle(IsAddressTrackedQuery request, CancellationToken cancellationToken)
    {
        TrackedAddress? trackedAddress = await dbContext.TrackedAddresses.AsNoTracking()
            .Where(x => x.Address == request.Address)
            .FirstOrDefaultAsync(cancellationToken);

        return trackedAddress is { IsTrackingActive: true };
    }
}