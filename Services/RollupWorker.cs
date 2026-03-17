using Microsoft.EntityFrameworkCore;
using LimsProject.Models;

namespace LimsProject.Services
{
    public class RollupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RollupWorker> _logger;

        public RollupWorker(IServiceScopeFactory scopeFactory, ILogger<RollupWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            _logger.LogInformation("Worker iniciado em: {time}", DateTimeOffset.Now);
            while (!stoppingToken.IsCancellationRequested)
            {
                using (IServiceScope scope = _scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    _logger.LogInformation("Iniciando processamento de rollup: {time}", DateTimeOffset.Now);
                    await ConsolidateSensorData(db);
                }
                // Espera 1 minuto antes de processar de novo (para teste, depois aumentamos)
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ConsolidateSensorData(AppDbContext db)
        {// 1. Pegamos a data de hoje (sem as horas)
            var today = DateTime.UtcNow.Date;

            // 2. Lógica de Agregação (GroupBy):
            // Aqui pedimos para o Postgres calcular a média de cada lote sozinho!
            var dailyAverages = await db.SensorData
                .Where(l => l.ReadingTime.Date == today)
                .GroupBy(l => l.BatchId)
                .Select(group => new
                {
                    BatchId = group.Key,
                    AvgTemperature = group.Average(l => l.Temperature)
                })
                .ToListAsync();

            foreach (var item in dailyAverages)
            {
                // 3. Verificamos se já existe um resumo para esse lote hoje
                var existingSummary = await db.Batches
                    .FirstOrDefaultAsync(s => s.Id == item.BatchId && s.CreatedAt == today);

                if (existingSummary != null)
                {
                    existingSummary.AvarageTemperature = item.AvgTemperature;
                    _logger.LogInformation($"Atualizado: Lote {item.BatchId} | Média: {item.AvgTemperature}");
                }
                else
                {
                    db.Batches.Add(new Batch
                    {
                        Id = item.BatchId,
                        AvarageTemperature= item.AvgTemperature,
                        CreatedAt = today
                    });
                    _logger.LogInformation($"Criado: Lote {item.BatchId} | Média: {item.AvgTemperature}");
                }
            }

            await db.SaveChangesAsync();
        }
    }
}
