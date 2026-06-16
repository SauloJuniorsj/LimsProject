using LimsProject.Application.Interfaces;
using LimsProject.Domain.Entities;
using LimsProject.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimsProject.API.Controllers;

[ApiController]
[Route("debug")]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize(Policy = "AdminOnly")]
public class DebugController(ILimsDbContext db, IHostEnvironment env) : ControllerBase
{
    private const int BatchesPerStatus = 2;
    private const int SensorReadingsPerBatch = 30;
    private const int DailySummaryDaysBack = 14;
    private const int SeedRandomSeed = 42;

    private static readonly string[] Strains =
    [
        "Purple Basil", "White Widow", "Mint", "Dill",
        "Lavender Haze", "Citrus Sunrise", "Pine Forest", "Mango Tango",
        "Northern Lights", "Strawberry Cough", "Blueberry Dream", "Green Crack",
    ];

    [HttpPost("populate-elegant")]
    public async Task<IResult> PopulateElegant()
    {
        if (!env.IsDevelopment() && !env.IsEnvironment("Testing"))
            return Results.NotFound();

        var random = new Random(SeedRandomSeed);
        var now = DateTime.UtcNow;
        var stats = new SeedStats();
        var statuses = Enum.GetValues<BatchStatus>();

        for (int s = 0; s < statuses.Length; s++)
        {
            for (int i = 0; i < BatchesPerStatus; i++)
            {
                SeedOneBatch(statuses[s], strainIndex: s * 2 + i, random, now, stats);
            }
        }

        await db.SaveChangesAsync();
        return Results.Ok(new
        {
            message = "Seed populado com dados ricos pra demo!",
            batches = stats.Batches,
            statusHistoryEntries = stats.History,
            sensorReadings = stats.Readings,
            dailySummaries = stats.Summaries,
            labAnalyses = stats.Analyses,
        });
    }

    private void SeedOneBatch(BatchStatus status, int strainIndex, Random random, DateTime now, SeedStats stats)
    {
        var daysAgo = DaysAgoForStatus(status, random);
        var createdAt = now.AddDays(-daysAgo);
        var batch = BuildBatch(status, strainIndex, createdAt, random);
        db.Batches.Add(batch);
        stats.Batches++;

        SeedStatusHistory(batch, status, createdAt, daysAgo, stats);
        SeedSensorReadings(batch, createdAt, daysAgo, random, stats);
        SeedDailySummaries(batch, createdAt, now, random, stats);
        SeedAnalysisIfTested(batch, status, createdAt, daysAgo, random, stats);
    }

    private static int DaysAgoForStatus(BatchStatus status, Random random) => status switch
    {
        BatchStatus.Released or BatchStatus.Rejected => 60 + random.Next(30),
        BatchStatus.Testing => 30 + random.Next(15),
        BatchStatus.Harvested => 20 + random.Next(10),
        BatchStatus.Growth => 10 + random.Next(10),
        _ => random.Next(5),
    };

    private static Batch BuildBatch(BatchStatus status, int strainIndex, DateTime createdAt, Random random)
    {
        var hasLabValues = status >= BatchStatus.Testing;
        return new Batch
        {
            Strain = Strains[strainIndex],
            Status = status,
            CreatedAt = createdAt,
            CreatedBy = "seed@demo.com",
            CurrentTemperature = 20 + (decimal)(random.NextDouble() * 8),
            CurrentMoisture = 50 + (decimal)(random.NextDouble() * 30),
            ThcPercentage = hasLabValues ? 0.2m + (decimal)(random.NextDouble() * 0.15) : null,
            CbdPercentage = hasLabValues ? 5 + (decimal)(random.NextDouble() * 8) : null,
        };
    }

    private void SeedStatusHistory(Batch batch, BatchStatus status, DateTime createdAt, int daysAgo, SeedStats stats)
    {
        BatchStatus? prev = null;
        var path = StatusPath(status);
        for (int p = 0; p < path.Length; p++)
        {
            var stepDate = createdAt.AddDays(p * (daysAgo / Math.Max(path.Length, 1)));
            db.BatchStatusHistories.Add(new BatchStatusHistory
            {
                BatchId = batch.Id,
                FromStatus = prev,
                ToStatus = path[p],
                ChangedAt = stepDate,
                ChangedBy = "seed@demo.com",
                Reason = prev is null ? "Lote criado (seed)" : null,
            });
            stats.History++;
            prev = path[p];
        }
    }

    private void SeedSensorReadings(Batch batch, DateTime createdAt, int daysAgo, Random random, SeedStats stats)
    {
        for (int r = 0; r < SensorReadingsPerBatch; r++)
        {
            db.SensorData.Add(new SensorData
            {
                BatchId = batch.Id,
                Temperature = 20 + (decimal)(random.NextDouble() * 12),
                ReadingTime = createdAt.AddSeconds(random.NextDouble() * daysAgo * 86400),
            });
            stats.Readings++;
        }
    }

    private void SeedDailySummaries(Batch batch, DateTime createdAt, DateTime now, Random random, SeedStats stats)
    {
        var today = now.Date;
        for (int d = 0; d < DailySummaryDaysBack; d++)
        {
            var day = today.AddDays(-d);
            if (day < createdAt.Date) break;

            var temps = Enumerable
                .Range(0, 24)
                .Select(_ => 19 + random.NextDouble() * 10)
                .ToList();

            db.BatchesDailySummaries.Add(new BatchDailySummary
            {
                BatchId = batch.Id,
                Date = day,
                AvgTemperature = (decimal)temps.Average(),
                MinTemperature = (decimal)temps.Min(),
                MaxTemperature = (decimal)temps.Max(),
                ReadingCount = temps.Count,
            });
            stats.Summaries++;
        }
    }

    private void SeedAnalysisIfTested(Batch batch, BatchStatus status, DateTime createdAt, int daysAgo, Random random, SeedStats stats)
    {
        if (status < BatchStatus.Testing) return;

        var passed = status == BatchStatus.Released;
        db.LabAnalyses.Add(new LabAnalysis
        {
            BatchId = batch.Id,
            THC = passed ? 0.25m : 0.45m,
            CBD = 8 + (decimal)(random.NextDouble() * 5),
            Terpenes = "myrcene, limonene, caryophyllene",
            IsPassed = passed,
            AnalysisDate = createdAt.AddDays(daysAgo - 2),
        });
        stats.Analyses++;
    }

    private static BatchStatus[] StatusPath(BatchStatus final) => final switch
    {
        BatchStatus.Germination => [BatchStatus.Germination],
        BatchStatus.Growth => [BatchStatus.Germination, BatchStatus.Growth],
        BatchStatus.Harvested =>
            [BatchStatus.Germination, BatchStatus.Growth, BatchStatus.Harvested],
        BatchStatus.Testing =>
            [BatchStatus.Germination, BatchStatus.Growth, BatchStatus.Harvested, BatchStatus.Testing],
        BatchStatus.Released =>
            [BatchStatus.Germination, BatchStatus.Growth, BatchStatus.Harvested, BatchStatus.Testing, BatchStatus.Released],
        BatchStatus.Rejected =>
            [BatchStatus.Germination, BatchStatus.Growth, BatchStatus.Harvested, BatchStatus.Testing, BatchStatus.Rejected],
        _ => [final],
    };

    private sealed class SeedStats
    {
        public int Batches { get; set; }
        public int History { get; set; }
        public int Readings { get; set; }
        public int Summaries { get; set; }
        public int Analyses { get; set; }
    }
}
