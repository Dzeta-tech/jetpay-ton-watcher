using BloomFilter;
using JetPay.TonWatcher.Domain.Entities;
using JetPay.TonWatcher.Infrastructure.Persistence;
using MediatR;
using Ton.Core.Addresses;

namespace JetPay.TonWatcher.Application.Commands;

public record AddTrackedAddressCommand : IRequest
{
    public required Address Address { get; init; }
}

public class AddTrackedAddressCommandHandler(
    ApplicationDbContext dbContext,
    IBloomFilter bloomFilter)
    : IRequestHandler<AddTrackedAddressCommand>
{
    public async Task Handle(AddTrackedAddressCommand request, CancellationToken cancellationToken)
    {
        TrackedAddress trackedAddress = TrackedAddress.Create(request.Address);
        await dbContext.TrackedAddresses.AddAsync(trackedAddress, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await bloomFilter.AddAsync(request.Address.Hash.ToArray());
    }
}