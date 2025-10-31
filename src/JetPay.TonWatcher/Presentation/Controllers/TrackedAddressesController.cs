using JetPay.TonWatcher.Application.Commands;
using JetPay.TonWatcher.Application.Common;
using JetPay.TonWatcher.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ton.Core.Addresses;

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

    /// <summary>
    ///     Add an address to track. Address can be in any format (raw 0:hex, base64, etc.)
    /// </summary>
    [HttpPost("add/{address}")]
    public async Task<IActionResult> AddTrackedAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
            return BadRequest(ApiResponse.FailureResult("Address is required"));

        try
        {
            // Parse address from string at API boundary
            Address parsedAddress = Address.Parse(address);

            await mediator.Send(new AddTrackedAddressCommand { Address = parsedAddress });

            return Ok(ApiResponse.SuccessResult());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.FailureResult($"Invalid address format: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Disable tracking for an address. Address can be in any format (raw 0:hex, base64, etc.)
    /// </summary>
    [HttpPost("disable/{address}")]
    public async Task<IActionResult> DisableTrackedAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
            return BadRequest(ApiResponse.FailureResult("Address is required"));

        try
        {
            // Parse address from string at API boundary
            Address parsedAddress = Address.Parse(address);
            ;

            bool success = await mediator.Send(new DisableTrackedAddressCommand { Address = parsedAddress });

            if (!success)
                return BadRequest(ApiResponse.FailureResult("Failed to disable address - address not found"));

            return Ok(ApiResponse.SuccessResult());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.FailureResult($"Invalid address format: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Check if an address is being tracked (returns true if exists and enabled, false otherwise).
    ///     Address can be in any format (raw 0:hex, base64, etc.)
    /// </summary>
    [HttpGet("is-tracked/{address}")]
    public async Task<IActionResult> IsAddressTracked(string address)
    {
        if (string.IsNullOrEmpty(address))
            return BadRequest(ApiResponse.FailureResult("Address is required"));

        try
        {
            // Parse address from string at API boundary
            Address parsedAddress = Address.Parse(address);

            bool isTracked = await mediator.Send(new IsAddressTrackedQuery { Address = parsedAddress });

            return Ok(ApiResponse<bool>.SuccessResult(isTracked));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.FailureResult($"Invalid address format: {ex.Message}"));
        }
    }
}