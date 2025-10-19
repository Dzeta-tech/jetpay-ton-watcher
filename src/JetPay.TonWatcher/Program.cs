using JetPay.TonWatcher.Configuration;
using Serilog;

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.UseLogging();
    builder.UseConfiguration();
    builder.UseDatabase();
    builder.UseControllers();
    builder.UseServices();

    WebApplication app = builder.Build();

    await app.UseDatabaseMigrations();

    app.UseControllers();

    await app.InitializeServices();

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