using BloomFilter;
using JetPay.TonWatcher.Domain.Entities;
using JetPay.TonWatcher.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace JetPay.TonWatcher.Configuration;

public static class WebApplicationExtensions
{
    public static void UseControllers(this WebApplication app)
    {
        app.UseRouting();
        app.MapControllers();
    }

    public static async Task UseDatabaseMigrations(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        for (int attempt = 1; attempt <= 5; attempt++)
            try
            {
                await dbContext.Database.MigrateAsync();

                Log.Information("Database migrations completed successfully");
                return;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Database migration attempt {Attempt}/5 failed", attempt);

                if (attempt >= 5)
                {
                    Log.Fatal(ex, "Failed to migrate database after 5 attempts");
                    throw;
                }

                TimeSpan delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                Log.Information("Retrying database migration in {Delay} seconds...", delay.TotalSeconds);
                await Task.Delay(delay);
            }
    }

    public static async Task InitializeServices(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();

        // Initialize Bloom Filter with existing tracked addresses
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        IBloomFilter bloomFilter = scope.ServiceProvider.GetRequiredService<IBloomFilter>();
        TrackedAddress[] trackedAddresses = await dbContext.TrackedAddresses
            .Where(x => x.IsTrackingActive)
            .ToArrayAsync();

        foreach (TrackedAddress trackedAddress in trackedAddresses) await bloomFilter.AddAsync(trackedAddress.Address.Hash);

        Log.Information("Initialized bloom filter with {Count} tracked addresses", trackedAddresses.Length);
    }
}