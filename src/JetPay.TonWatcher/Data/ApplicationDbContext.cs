using JetPay.TonWatcher.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace JetPay.TonWatcher.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<TrackedAddress> TrackedAddresses { get; set; } = null!;
    public DbSet<ShardBlock> ShardBlocks { get; set; } = null!;
}