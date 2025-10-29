using BloomFilter;
using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Domain.Entities;
using MediatR;

namespace JetPay.TonWatcher.Application.Commands.AddTrackedAddress;

public class AddTrackedAddressCommandHandler(
    ITrackedAddressRepository trackedAddressRepository,
    IBloomFilter bloomFilter,
    ILogger<AddTrackedAddressCommandHandler> logger)
    : IRequestHandler<AddTrackedAddressCommand>
{
    public async Task Handle(AddTrackedAddressCommand request,
        CancellationToken cancellationToken)
    {
        TrackedAddress trackedAddress = TrackedAddress.Create(request.Address);

        await trackedAddressRepository.AddAsync(trackedAddress, cancellationToken);
        await bloomFilter.AddAsync(request.Address.Hash.ToArray());

        logger.LogInformation("Added tracked address {Address} with ID {Id}",
            request.Address.ToString(), trackedAddress.Id);
    }
}