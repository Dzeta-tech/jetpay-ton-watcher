using JetPay.TonWatcher.Data;
using JetPay.TonWatcher.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace JetPay.TonWatcher.Controllers;

public class ApiController(ILogger<ApiController> logger, ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet("api/v1/status")]
    public IActionResult GetStatus()
    {
        return Ok("OK");
    }

    [HttpPost("api/v1/tracked-addresses/add/{address}")]
    public async Task<IActionResult> AddTrackedAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
            return BadRequest("Address is required");

        if (address.Length != 66)
            return BadRequest("Address must be 66 characters long");

        if (!address.StartsWith("0:"))
            return BadRequest("Address must start with 0:");

        TrackedAddress trackedAddress = new() { Address = address };

        await dbContext.TrackedAddresses.AddAsync(trackedAddress);
        await dbContext.SaveChangesAsync();

        return Ok();
    }
}