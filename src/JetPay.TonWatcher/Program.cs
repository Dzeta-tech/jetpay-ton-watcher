using JetPay.TonWatcher.Configuration;
using JetPay.TonWatcher.Presentation.Services;
using Serilog;

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.UseLogging();
    builder.UseConfiguration();
    builder.UseDatabase();
    builder.UseNats();
    builder.UseGrpc();
    builder.UseHealthChecks();
    builder.UseServices();

    WebApplication app = builder.Build();

    await app.UseDatabaseMigrations();
    await app.InitializeServices();

    app.MapGrpcService<TonWatcherService>();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}