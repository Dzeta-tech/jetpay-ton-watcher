using MediatR;
using TonSdk.Core;

namespace JetPay.TonWatcher.Application.Commands.DisableTrackedAddress;

public record DisableTrackedAddressCommand : IRequest<bool>
{
    public required Address Address { get; init; }
}

