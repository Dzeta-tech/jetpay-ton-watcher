using JetPay.TonWatcher.Application.Commands.SyncShardBlocks;
using JetPay.TonWatcher.Application.Interfaces;
using MediatR;

namespace JetPay.TonWatcher.Infrastructure.BackgroundServices;

public class MasterchainSyncService(
    ILogger<MasterchainSyncService> logger,
    IServiceScopeFactory scopeFactory,
    ILiteClientService liteClientService) : BackgroundService
{
    readonly TimeSpan syncInterval = TimeSpan.FromMilliseconds(100);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!liteClientService.IsConnected())
                {
                    logger.LogWarning("LiteClient not connected, attempting reconnection...");
                    try
                    {
                        await liteClientService.InitializeAsync();
                        logger.LogInformation("LiteClient reconnected successfully");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to reconnect LiteClient, will retry");
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        continue;
                    }
                }

                using IServiceScope scope = scopeFactory.CreateScope();
                IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                SyncShardBlocksResult result = await mediator.Send(new SyncShardBlocksCommand(), stoppingToken);

                if (result.Success && result.BlocksAdded > 0)
                    logger.LogInformation("Synced {Count} new shard blocks", result.BlocksAdded);
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning(ex, "Timeout in masterchain sync, will retry");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in masterchain sync, will retry");
            }

            await Task.Delay(syncInterval, stoppingToken);
        }
    }
}