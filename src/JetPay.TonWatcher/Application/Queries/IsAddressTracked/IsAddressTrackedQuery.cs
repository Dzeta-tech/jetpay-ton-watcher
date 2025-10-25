using MediatR;
using TonSdk.Core;

namespace JetPay.TonWatcher.Application.Queries.IsAddressTracked;

public record IsAddressTrackedQuery : IRequest<bool>
{
    public required Address Address { get; init; }
}

