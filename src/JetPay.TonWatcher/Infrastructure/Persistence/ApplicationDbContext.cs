using JetPay.TonWatcher.Domain.Entities;
using JetPay.TonWatcher.Infrastructure.Persistence.Attributes;
using Microsoft.EntityFrameworkCore;
using TonSdk.Core;

namespace JetPay.TonWatcher.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<TrackedAddress> TrackedAddresses { get; set; } = null!;
    public DbSet<ShardBlock> ShardBlocks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Address conversion for TrackedAddress entity
        modelBuilder.Entity<TrackedAddress>()
            .Property(e => e.Address)
            .HasConversion(AddressConversionAttribute.GetConverter())
            .HasMaxLength(36); // 4 bytes (int32 workchain) + 32 bytes (hash)
    }
}