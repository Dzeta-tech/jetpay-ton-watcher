using BloomFilter;
using JetPay.TonWatcher.Domain.Entities;
using JetPay.TonWatcher.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace JetPay.TonWatcher.Configuration;

public static class WebApplicationExtensions
{
    public static async Task UseDatabaseMigrations(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public static async Task InitializeServices(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        IBloomFilter bloomFilter = scope.ServiceProvider.GetRequiredService<IBloomFilter>();

        TrackedAddress[] trackedAddresses = await dbContext.TrackedAddresses
            .Where(x => x.IsTrackingActive)
            .ToArrayAsync();

        foreach (TrackedAddress trackedAddress in trackedAddresses)
            await bloomFilter.AddAsync(trackedAddress.Address.Hash);

        Log.Information("Initialized bloom filter with {Count} tracked addresses", trackedAddresses.Length);
    }
}