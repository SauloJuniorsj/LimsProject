using LimsProject.Services;

public class RollupWorker(IServiceScopeFactory scopeFactory, ILogger<RollupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Executando consolidação de dados às: {time}", DateTimeOffset.Now);

            using (var scope = scopeFactory.CreateScope())
            {
                var rollupService = scope.ServiceProvider.GetRequiredService<IRollupService>();
                await rollupService.ConsolidateDataAsync(stoppingToken);
            }

            // Espera 1 minuto antes da próxima execução
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}