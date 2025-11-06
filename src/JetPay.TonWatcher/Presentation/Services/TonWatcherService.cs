using Grpc.Core;
using JetPay.TonWatcher.Application.Commands;
using JetPay.TonWatcher.Application.Queries;
using MediatR;
using Ton.Core.Addresses;
using TonWatcher.Grpc;

namespace JetPay.TonWatcher.Presentation.Services;

public class TonWatcherService(IMediator mediator) : global::TonWatcher.Grpc.TonWatcher.TonWatcherBase
{
    public override async Task<AddTrackedAddressResponse> AddTrackedAddress(
        AddTrackedAddressRequest request,
        ServerCallContext context)
    {
        Address parsedAddress = Address.Parse(request.Address);
        await mediator.Send(new AddTrackedAddressCommand { Address = parsedAddress }, context.CancellationToken);
        return new AddTrackedAddressResponse { Success = true };
    }

    public override async Task<DisableTrackedAddressResponse> DisableTrackedAddress(
        DisableTrackedAddressRequest request,
        ServerCallContext context)
    {
        Address parsedAddress = Address.Parse(request.Address);
        bool success = await mediator.Send(
            new DisableTrackedAddressCommand { Address = parsedAddress },
            context.CancellationToken);

        return new DisableTrackedAddressResponse { Success = success };
    }

    public override async Task<IsAddressTrackedResponse> IsAddressTracked(
        IsAddressTrackedRequest request,
        ServerCallContext context)
    {
        Address parsedAddress = Address.Parse(request.Address);
        bool isTracked = await mediator.Send(
            new IsAddressTrackedQuery { Address = parsedAddress },
            context.CancellationToken);

        return new IsAddressTrackedResponse { IsTracked = isTracked };
    }

    public override Task<StatusResponse> GetStatus(StatusRequest request, ServerCallContext context)
    {
        return Task.FromResult(new StatusResponse { Success = true });
    }
}