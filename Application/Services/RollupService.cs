using LimsProject.Application.Interfaces;
using LimsProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Application.Services;

public interface IRollupService
{
    Task ConsolidateDataAsync(CancellationToken ct);
}

public class RollupService(ILimsDbContext db) : IRollupService
{
    public async Task ConsolidateDataAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var activeBatches = await db.Batches.AsNoTracking().ToListAsync(ct);

        foreach (var batch in activeBatches)
        {
            var average = await db.SensorData
                .Where(s => s.BatchId == batch.Id && s.ReadingTime.Date == today)
                .AverageAsync(s => (decimal?)s.Temperature, ct) ?? 0;

            if (average == 0) continue;

            var existingSummary = await db.BatchesDailySummaries
                .FirstOrDefaultAsync(s => s.BatchId == batch.Id && s.Date == today, ct);

            if (existingSummary is null)
            {
                db.BatchesDailySummaries.Add(new BatchDailySummary
                {
                    BatchId = batch.Id,
                    AvgTemperature = average,
                    Date = today
                });
            }
            else
            {
                existingSummary.AvgTemperature = average;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
