using LimsProject.Models;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Services;

public interface IRollupService
{
    Task ConsolidateDataAsync(CancellationToken ct);
}

public class RollupService(AppDbContext db) : IRollupService
{
    public async Task ConsolidateDataAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;

        // 1. Adicionamos .AsNoTracking() aqui. Isso evita o conflito de "Tracking"
        var activeBatches = await db.Batches.AsNoTracking().ToListAsync(ct);

        foreach (var batch in activeBatches)
        {
            var average = await db.SensorData
                .Where(s => s.BatchId == batch.Id && s.ReadingTime.Date == today)
                .AverageAsync(s => (decimal?)s.Temperature, ct) ?? 0;

            if (average > 0)
            {
                var existingSummary = await db.BatchesDailySumaries
                    .FirstOrDefaultAsync(s => s.Id == batch.Id && s.Date == today, ct);

                if (existingSummary == null)
                {
                    // 2. ATENÇÃO: Adicione na tabela de SUMMARIES
                    db.BatchesDailySumaries.Add(new BatchDailySumarry// <--- Troquei aqui
                    {
                        BatchId = batch.Id,
                        AvgTemperature = average, // Use o nome da propriedade da sua model de resumo
                        Date = today
                    });
                }
                else
                {
                    existingSummary.AvgTemperature = average;
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }
}