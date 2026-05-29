using LimsProject.Application.Services;
using Microsoft.Extensions.Configuration;

namespace LimsProject.Application.Workers;

public class RollupWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<RollupWorker> logger,
    IConfiguration config) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(
        config.GetValue<int>("Rollup:IntervalSeconds", 60));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Executando consolidação de dados às: {Time}", DateTimeOffset.Now);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var rollupService = scope.ServiceProvider.GetRequiredService<IRollupService>();
                await rollupService.ConsolidateDataAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha na consolidação de dados. Próxima tentativa em {Interval}", _interval);
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
