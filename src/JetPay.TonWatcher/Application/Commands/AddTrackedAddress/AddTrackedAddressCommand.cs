using MediatR;

namespace JetPay.TonWatcher.Application.Commands.AddTrackedAddress;

public record AddTrackedAddressCommand : IRequest<AddTrackedAddressResult>
{
    public required string Address { get; init; }
}

public record AddTrackedAddressResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? AddressId { get; init; }
}