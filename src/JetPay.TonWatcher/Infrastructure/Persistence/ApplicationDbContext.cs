using JetPay.TonWatcher.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JetPay.TonWatcher.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<TrackedAddress> TrackedAddresses { get; set; } = null!;
    public DbSet<ShardBlock> ShardBlocks { get; set; } = null!;
}

