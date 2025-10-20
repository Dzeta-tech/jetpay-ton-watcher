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
    public async Task<IActionResult> AddTrackedAddress(string address)
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

        TrackedAddress trackedAddress = new() 
        { 
            Workchain = parsedAddress.GetWorkchain(),
            Account = parsedAddress.GetHash()
        };

        await dbContext.TrackedAddresses.AddAsync(trackedAddress);
        await dbContext.SaveChangesAsync();

        await addressBloomFilter.AddAsync(parsedAddress.GetHash());

        return Ok("Address added to tracking");
    }
}