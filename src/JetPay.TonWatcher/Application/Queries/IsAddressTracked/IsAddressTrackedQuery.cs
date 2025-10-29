using MediatR;
using Ton.Core.Addresses;

namespace JetPay.TonWatcher.Application.Queries.IsAddressTracked;

public record IsAddressTrackedQuery : IRequest<bool>
{
    public required Address Address { get; init; }
}