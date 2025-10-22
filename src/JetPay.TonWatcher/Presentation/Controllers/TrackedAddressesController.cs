using JetPay.TonWatcher.Application.Commands.AddTrackedAddress;
using JetPay.TonWatcher.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace JetPay.TonWatcher.Presentation.Controllers;

[ApiController]
[Route("api/v1/tracked-addresses")]
public class TrackedAddressesController(IMediator mediator) : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(ApiResponse.SuccessResult());
    }

    [HttpPost("add/{address}")]
    public async Task<IActionResult> AddTrackedAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            return BadRequest(ApiResponse.FailureResult("Address is required"));
        }

        AddTrackedAddressResult result = await mediator.Send(new AddTrackedAddressCommand { Address = address });

        if (!result.Success)
        {
            return BadRequest(ApiResponse.FailureResult(result.ErrorMessage ?? "Failed to add address"));
        }

        return Ok(ApiResponse<Guid>.SuccessResult(result.AddressId!.Value));
    }
}

