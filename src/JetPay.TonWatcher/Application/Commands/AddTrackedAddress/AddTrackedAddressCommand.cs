using MediatR;
using Ton.Core.Addresses;

namespace JetPay.TonWatcher.Application.Commands.AddTrackedAddress;

public record AddTrackedAddressCommand : IRequest
{
    public required Address Address { get; init; }
}