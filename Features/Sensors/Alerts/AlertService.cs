using LimsProject.Common.Persistence;
using LimsProject.Features.Traceability;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Features.Sensors.Alerts;

public sealed class AlertService(AppDbContext db, IChainOfCustodyWriter coc) : IAlertService
{
    public async Task EvaluateBulkAsync(IReadOnlyList<SensorData> rows, CancellationToken ct)
    {
        if (rows.Count == 0)
            return;

        var batchIds = rows.Select(r => r.BatchId).Distinct().ToList();
        var strainByBatch = await (
            from b in db.Batches.AsNoTracking()
            join sl in db.SeedLots.AsNoTracking() on b.SeedLotId equals sl.Id
            where batchIds.Contains(b.Id)
            select new { b.Id, sl.StrainId }).ToDictionaryAsync(x => x.Id, x => x.StrainId, ct);

        var strainIds = strainByBatch.Values.Distinct().ToList();
        var thresholdRows = await db.AlertThresholds.AsNoTracking()
            .Where(t => strainIds.Contains(t.StrainId))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        var thresholds = thresholdRows
            .GroupBy(t => t.StrainId)
            .ToDictionary(g => g.Key, g => g.First());

        var alerts = new List<EnvironmentalAlert>();
        foreach (var row in rows)
        {
            if (!strainByBatch.TryGetValue(row.BatchId, out var strainId))
                continue;
            if (!thresholds.TryGetValue(strainId, out var th))
                continue;

            string? msg = null;
            if (row.Temperature < th.MinTemperature || row.Temperature > th.MaxTemperature)
                msg = $"Temperatura {row.Temperature} fora do intervalo [{th.MinTemperature}, {th.MaxTemperature}].";
            else if (row.Humidity.HasValue && th.MaxHumidity.HasValue && row.Humidity > th.MaxHumidity)
                msg = $"Umidade {row.Humidity} acima do máximo {th.MaxHumidity}.";

            if (msg is null)
                continue;

            alerts.Add(new EnvironmentalAlert
            {
                BatchId = row.BatchId,
                SensorDataId = row.Id,
                Message = msg,
            });
            coc.Append(row.BatchId, CocEventTypes.AlertRaised, new { msg }, null);
        }

        if (alerts.Count > 0)
        {
            db.EnvironmentalAlerts.AddRange(alerts);
            await db.SaveChangesAsync(ct);
        }
    }
}
