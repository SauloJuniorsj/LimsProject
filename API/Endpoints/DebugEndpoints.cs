using LimsProject.Application.Interfaces;
using LimsProject.Domain.Entities;
using LimsProject.Domain.Enums;

namespace LimsProject.API.Endpoints;

public static class DebugEndpoints
{
    private static readonly string[] Strains =
    [
        "Purple Basil", "White Widow", "Mint", "Dill",
        "Lavender Haze", "Citrus Sunrise", "Pine Forest", "Mango Tango",
        "Northern Lights", "Strawberry Cough", "Blueberry Dream", "Green Crack",
    ];

    public static void MapDebugEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
            return;

        app.MapPost("/debug/populate-elegant", async (ILimsDbContext db) =>
        {
            var random = new Random(42); // seed determinístico
            var now = DateTime.UtcNow;
            var statuses = Enum.GetValues<BatchStatus>();
            var counts = new
            {
                batches = 0,
                history = 0,
                readings = 0,
                summaries = 0,
                analyses = 0,
            };

            int batchCount = 0, historyCount = 0, readingCount = 0, summaryCount = 0, analysisCount = 0;

            // 2 batches por status — pipeline visualmente rica em estados variados
            for (int s = 0; s < statuses.Length; s++)
            {
                var status = statuses[s];
                for (int i = 0; i < 2; i++)
                {
                    // Backdating realista: estados terminais são mais antigos
                    var daysAgo = status switch
                    {
                        BatchStatus.Released or BatchStatus.Rejected => 60 + random.Next(30),
                        BatchStatus.Testing => 30 + random.Next(15),
                        BatchStatus.Harvested => 20 + random.Next(10),
                        BatchStatus.Growth => 10 + random.Next(10),
                        _ => random.Next(5),
                    };
                    var createdAt = now.AddDays(-daysAgo);

                    var batch = new Batch
                    {
                        Strain = Strains[s * 2 + i],
                        Status = status,
                        CreatedAt = createdAt,
                        CreatedBy = "seed@demo.com",
                        CurrentTemperature = 20 + (decimal)(random.NextDouble() * 8),
                        CurrentMoisture = 50 + (decimal)(random.NextDouble() * 30),
                        ThcPercentage = status >= BatchStatus.Testing
                            ? 0.2m + (decimal)(random.NextDouble() * 0.15) : null,
                        CbdPercentage = status >= BatchStatus.Testing
                            ? 5 + (decimal)(random.NextDouble() * 8) : null,
                    };
                    db.Batches.Add(batch);
                    batchCount++;

                    // Reconstrói o status history backdated
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
                        historyCount++;
                        prev = path[p];
                    }

                    // 30 sensor readings espalhadas no período de vida
                    for (int r = 0; r < 30; r++)
                    {
                        db.SensorData.Add(new SensorData
                        {
                            BatchId = batch.Id,
                            Temperature = 20 + (decimal)(random.NextDouble() * 12),
                            ReadingTime = createdAt.AddSeconds(random.NextDouble() * daysAgo * 86400),
                        });
                        readingCount++;
                    }

                    // Daily summaries dos últimos 14 dias dentro do período de vida
                    var today = now.Date;
                    for (int d = 0; d < 14; d++)
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
                        summaryCount++;
                    }

                    // Análise pra batches que já passaram pelo lab
                    if (status >= BatchStatus.Testing)
                    {
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
                        analysisCount++;
                    }
                }
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Seed populado com dados ricos pra demo!",
                batches = batchCount,
                statusHistoryEntries = historyCount,
                sensorReadings = readingCount,
                dailySummaries = summaryCount,
                labAnalyses = analysisCount,
            });
        }).RequireAuthorization("AdminOnly");
    }

    /// <summary>Caminho de transições até o status final, incluindo o próprio final.</summary>
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
}
