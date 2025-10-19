using System.Text.Json.Serialization;
using Dzeta.Configuration;
using JetPay.TonWatcher.Data;
using JetPay.TonWatcher.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

namespace JetPay.TonWatcher.Configuration;

public static class DependencyInjection
{
    public static void UseLogging(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        builder.Host.UseSerilog();
    }

    public static void UseConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.UseEnvironmentConfigurationProvider();
        builder.Services.AddConfiguration<AppConfiguration>();
    }

    public static void UseDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            AppConfiguration? config = serviceProvider.GetRequiredService<AppConfiguration>();
            options.UseNpgsql(config.Database.ConnectionString,
                npgsqlDbContextOptionsBuilder =>
                {
                    npgsqlDbContextOptionsBuilder.ConfigureDataSource(x => x.EnableDynamicJson());
                });
        });
    }

    public static void UseControllers(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers().AddJsonOptions(opts =>
        {
            JsonStringEnumConverter enumConverter = new();
            opts.JsonSerializerOptions.Converters.Add(enumConverter);
        });
    }

    public static void UseServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ITonClientFactory, OrbsTonClientFactory>();
        builder.Services.AddBloomFilter(setupAction => { setupAction.UseInMemory(); });
        builder.Services.AddHostedService<MasterchainSyncService>();
        builder.Services.AddHostedService<BlockProcessor>();
    }
}