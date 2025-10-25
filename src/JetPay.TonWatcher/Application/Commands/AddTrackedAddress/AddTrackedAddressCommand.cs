using MediatR;
using TonSdk.Core;

namespace JetPay.TonWatcher.Application.Commands.AddTrackedAddress;

public record AddTrackedAddressCommand : IRequest<AddTrackedAddressResult>
{
    public required Address Address { get; init; }
}

public record AddTrackedAddressResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? AddressId { get; init; }
}