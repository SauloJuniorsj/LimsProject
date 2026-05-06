using LimsProject.Common.Persistence;
using LimsProject.Common.Results;
using LimsProject.Features.Sensors.Alerts;
using LimsProject.Features.Traceability;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Features.Sensors;

public sealed class SensorIngestionService(
    AppDbContext db,
    IAlertService alerts,
    IChainOfCustodyWriter coc) : ISensorIngestionService
{
    public async Task<Result<int>> IngestBulkAsync(BulkSensorRequest request, CancellationToken ct)
    {
        if (request.Readings.Count == 0)
            return Result<int>.Failure("Nenhuma leitura.");

        var distinctBatchIds = request.Readings.Select(r => r.BatchId).Distinct().ToList();
        var existingCount = await db.Batches.AsNoTracking()
            .CountAsync(b => distinctBatchIds.Contains(b.Id), ct);
        if (existingCount != distinctBatchIds.Count)
            return Result<int>.Failure("Um ou mais lotes não existem.");

        var rows = new List<SensorData>(request.Readings.Count);
        foreach (var r in request.Readings)
        {
            var utc = r.ReadingTime.Kind == DateTimeKind.Utc ? r.ReadingTime : r.ReadingTime.ToUniversalTime();
            rows.Add(new SensorData
            {
                BatchId = r.BatchId,
                Temperature = r.Temperature,
                Humidity = r.Humidity,
                SensorType = SensorReadingKind.Temperature,
                ReadingTime = utc,
            });
        }

        db.SensorData.AddRange(rows);

        foreach (var bid in distinctBatchIds)
        {
            var n = rows.Count(x => x.BatchId == bid);
            coc.Append(bid, CocEventTypes.SensorsIngested, new { Count = n }, null);
        }

        await db.SaveChangesAsync(ct);
        await alerts.EvaluateBulkAsync(rows, ct);
        return Result<int>.Success(rows.Count);
    }
}
