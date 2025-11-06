using System.Reflection;
using Dzeta.Configuration;
using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Infrastructure.BackgroundServices;
using JetPay.TonWatcher.Infrastructure.Messaging;
using JetPay.TonWatcher.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NATS.Client.Core;
using Serilog;
using Serilog.Events;
using Ton.LiteClient;
using Ton.LiteClient.Engines;

namespace JetPay.TonWatcher.Configuration;

public static class DependencyInjection
{
    public static void UseLogging(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "TonWatcher")
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
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

    public static void UseNats(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<INatsConnection>(serviceProvider =>
        {
            AppConfiguration config = serviceProvider.GetRequiredService<AppConfiguration>();
            NatsOpts opts = NatsOpts.Default with { Url = config.Nats.Url };
            return new NatsConnection(opts);
        });

        builder.Services.AddScoped<IMessagePublisher, NatsJetStreamPublisher>();
    }

    public static void UseGrpc(this WebApplicationBuilder builder)
    {
        builder.Services.AddGrpc(options => { options.EnableDetailedErrors = builder.Environment.IsDevelopment(); });
    }

    public static void UseHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddNpgSql(
                serviceProvider => serviceProvider.GetRequiredService<AppConfiguration>().Database.ConnectionString,
                name: "database",
                tags: ["ready"]);
    }

    public static void UseServices(this WebApplicationBuilder builder)
    {
        // MediatR
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        builder.Services.AddSingleton<LiteClient>(serviceProvider =>
        {
            AppConfiguration config = serviceProvider.GetRequiredService<AppConfiguration>();
            byte[] publicKey = Convert.FromHexString(config.LiteClient.PublicKey);
            LiteSingleEngine engine = new(config.LiteClient.Host, config.LiteClient.Port, publicKey);
            RateLimitedLiteEngine rateLimitedEngine = new(engine, config.LiteClient.Ratelimit);
            return new LiteClient(rateLimitedEngine);
        });

        // Bloom Filter
        builder.Services.AddBloomFilter(setupAction => { setupAction.UseInMemory(); });

        // Background Services
        builder.Services.AddHostedService<MasterchainSyncService>();
        builder.Services.AddHostedService<BlockProcessorService>();
    }
}