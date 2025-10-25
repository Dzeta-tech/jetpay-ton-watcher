using BloomFilter;
using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Domain.Entities;
using MediatR;
using TonSdk.Core;

namespace JetPay.TonWatcher.Application.Commands.AddTrackedAddress;

public class AddTrackedAddressCommandHandler(
    ITrackedAddressRepository trackedAddressRepository,
    IBloomFilter bloomFilter,
    ILogger<AddTrackedAddressCommandHandler> logger)
    : IRequestHandler<AddTrackedAddressCommand, AddTrackedAddressResult>
{
    public async Task<AddTrackedAddressResult> Handle(AddTrackedAddressCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            TrackedAddress trackedAddress = TrackedAddress.Create(request.Address);

            await trackedAddressRepository.AddAsync(trackedAddress, cancellationToken);
            await bloomFilter.AddAsync(request.Address.Hash.ToArray());

            logger.LogInformation("Added tracked address {Address} with ID {Id}",
                request.Address.ToRaw(), trackedAddress.Id);

            return new AddTrackedAddressResult
            {
                Success = true,
                AddressId = trackedAddress.Id
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding tracked address {Address}", request.Address.ToRaw());
            return new AddTrackedAddressResult
            {
                Success = false,
                ErrorMessage = $"Failed to add address: {ex.Message}"
            };
        }
    }
}