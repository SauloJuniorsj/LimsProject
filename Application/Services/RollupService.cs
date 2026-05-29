using LimsProject.Application.Interfaces;
using LimsProject.Domain.Entities;
using LimsProject.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LimsProject.Application.Services;

public interface IRollupService
{
    Task ConsolidateDataAsync(CancellationToken ct);
}

public class RollupService(ILimsDbContext db, ILogger<RollupService> logger) : IRollupService
{
    public async Task ConsolidateDataAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;

        var activeBatches = await db.Batches
            .AsNoTracking()
            .Where(b => b.Status != BatchStatus.Released && b.Status != BatchStatus.Rejected)
            .ToListAsync(ct);

        int updated = 0;
        foreach (var batch in activeBatches)
        {
            var readings = await db.SensorData
                .Where(s => s.BatchId == batch.Id && s.ReadingTime.Date == today)
                .Select(s => s.Temperature)
                .ToListAsync(ct);

            if (readings.Count == 0) continue;

            var avg = readings.Average();
            var min = readings.Min();
            var max = readings.Max();

            var existing = await db.BatchesDailySummaries
                .FirstOrDefaultAsync(s => s.BatchId == batch.Id && s.Date == today, ct);

            if (existing is null)
            {
                db.BatchesDailySummaries.Add(new BatchDailySummary
                {
                    BatchId = batch.Id,
                    AvgTemperature = avg,
                    MinTemperature = min,
                    MaxTemperature = max,
                    ReadingCount = readings.Count,
                    Date = today
                });
            }
            else
            {
                existing.AvgTemperature = avg;
                existing.MinTemperature = min;
                existing.MaxTemperature = max;
                existing.ReadingCount = readings.Count;
            }

            updated++;
        }

        if (updated > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Consolidação concluída: {Count} lote(s) processados para {Date}", updated, today);
        }
    }
}
