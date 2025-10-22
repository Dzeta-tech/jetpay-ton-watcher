using JetPay.TonWatcher.Application.Commands.ProcessShardBlock;
using JetPay.TonWatcher.Application.Interfaces;
using JetPay.TonWatcher.Domain.Entities;
using MediatR;

namespace JetPay.TonWatcher.Infrastructure.BackgroundServices;

public class BlockProcessorService(
    ILogger<BlockProcessorService> logger,
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
                IShardBlockRepository shardBlockRepository = scope.ServiceProvider.GetRequiredService<IShardBlockRepository>();

                List<ShardBlock> unprocessedBlocks = await shardBlockRepository.GetUnprocessedAsync(100, stoppingToken);

                if (unprocessedBlocks.Count == 0)
                {
                    await Task.Delay(syncInterval, stoppingToken);
                    continue;
                }

                foreach (ShardBlock block in unprocessedBlocks)
                {
                    ProcessShardBlockResult result = await mediator.Send(
                        new ProcessShardBlockCommand { ShardBlockId = block.Id }, 
                        stoppingToken);

                    if (result.Success && result.TransactionsFound > 0)
                    {
                        logger.LogInformation("Processed block {Shard}:{Seqno}, found {Count} transactions",
                            block.Shard, block.Seqno, result.TransactionsFound);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in block processor, will retry");
            }

            await Task.Delay(syncInterval, stoppingToken);
        }
    }
}

