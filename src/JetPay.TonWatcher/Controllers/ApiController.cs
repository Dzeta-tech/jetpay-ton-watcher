using BloomFilter;
using JetPay.TonWatcher.Data;
using JetPay.TonWatcher.Data.Models;
using Microsoft.AspNetCore.Mvc;
using TonSdk.Core;

namespace JetPay.TonWatcher.Controllers;

public class ApiController(
    ILogger<ApiController> logger,
    ApplicationDbContext dbContext,
    IBloomFilter addressBloomFilter) : ControllerBase
{
    [HttpGet("api/v1/status")]
    public IActionResult GetStatus()
    {
        return Ok("OK");
    }

    [HttpPost("api/v1/tracked-addresses/add/{address}")]
    public async Task<IActionResult> AddTrackedAddress(string address, [FromQuery] string type = "TON")
    {
        if (string.IsNullOrEmpty(address))
            return BadRequest("Address is required");

        Address parsedAddress;
        try
        {
            parsedAddress = new Address(address);
        }
        catch (Exception ex)
        {
            return BadRequest($"Invalid address format: {ex.Message}");
        }

        if (!Enum.TryParse<TrackedAddressType>(type, true, out TrackedAddressType addressType))
            return BadRequest($"Invalid address type. Must be 'TON' or 'Jetton'");

        TrackedAddress trackedAddress = new() 
        { 
            Workchain = parsedAddress.GetWorkchain(),
            Account = parsedAddress.GetHash(),
            Type = addressType
        };

        await dbContext.TrackedAddresses.AddAsync(trackedAddress);
        await dbContext.SaveChangesAsync();

        await addressBloomFilter.AddAsync(parsedAddress.GetHash());

        return Ok($"{addressType} address added to tracking");
    }
}