using JetPay.TonWatcher.Application.Commands.SyncShardBlocks;
using MediatR;

namespace JetPay.TonWatcher.Infrastructure.BackgroundServices;

public class MasterchainSyncService(
    ILogger<MasterchainSyncService> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    readonly TimeSpan syncInterval = TimeSpan.FromMilliseconds(100);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                SyncShardBlocksResult result = await mediator.Send(new SyncShardBlocksCommand(), stoppingToken);

                if (result is { Success: true, BlocksAdded: > 0 })
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