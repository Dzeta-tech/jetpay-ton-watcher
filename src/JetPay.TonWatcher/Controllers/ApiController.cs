using BloomFilter;
using JetPay.TonWatcher.Data;
using JetPay.TonWatcher.Data.Models;
using Microsoft.AspNetCore.Mvc;
using TonSdk.Core;

namespace JetPay.TonWatcher.Controllers;

[Route("api/v1")]
public class ApiController(
    ApplicationDbContext dbContext,
    IBloomFilter addressBloomFilter) : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok("OK");
    }

    [HttpPost("tracked-addresses/add/{address}")]
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