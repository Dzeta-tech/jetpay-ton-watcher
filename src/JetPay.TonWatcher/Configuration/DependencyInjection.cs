using System.Reflection;
using System.Text.Json.Serialization;
using Dzeta.Configuration;
using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Infrastructure.BackgroundServices;
using JetPay.TonWatcher.Infrastructure.LiteClient;
using JetPay.TonWatcher.Infrastructure.Messaging;
using JetPay.TonWatcher.Infrastructure.Persistence;
using JetPay.TonWatcher.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;

namespace JetPay.TonWatcher.Configuration;

public static class DependencyInjection
{
    public static void UseLogging(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
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

    public static void UseRedis(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            AppConfiguration configuration = serviceProvider.GetRequiredService<AppConfiguration>();

            string redisOptions = $"{configuration.Redis.Host}:{configuration.Redis.Port}";

            if (!string.IsNullOrEmpty(configuration.Redis.User))
                redisOptions += ",user=" + configuration.Redis.User;

            if (!string.IsNullOrEmpty(configuration.Redis.Password))
                redisOptions += ",password=" + configuration.Redis.Password;

            return ConnectionMultiplexer.Connect(redisOptions);
        });

        builder.Services.AddScoped<RedisDatabase>(serviceProvider =>
        {
            IConnectionMultiplexer redis = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
            return redis.GetDatabase();
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
        // MediatR
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Repositories
        builder.Services.AddScoped<ITrackedAddressRepository, TrackedAddressRepository>();
        builder.Services.AddScoped<IShardBlockRepository, ShardBlockRepository>();

        // Infrastructure Services
        builder.Services.AddSingleton<ILiteClientService, LiteClientService>();
        builder.Services.AddScoped<IMessagePublisher, RedisStreamPublisher>();

        // Bloom Filter
        builder.Services.AddBloomFilter(setupAction => { setupAction.UseInMemory(); });

        // Background Services
        builder.Services.AddHostedService<MasterchainSyncService>();
        builder.Services.AddHostedService<BlockProcessorService>();
    }
}