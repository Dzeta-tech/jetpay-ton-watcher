using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Ton.Core.Addresses;

namespace JetPay.TonWatcher.Infrastructure.Persistence;

public class DesignTimeDbFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("Database__ConnectionString")
                                  ?? "Host=localhost;Database=jetpay_ton_watcher;Username=postgres;Password=postgres";

        DbContextOptionsBuilder<ApplicationDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}