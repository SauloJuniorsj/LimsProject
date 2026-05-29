using FluentAssertions;
using LimsProject.Application.Services;
using Xunit;
using LimsProject.Domain.Entities;
using LimsProject.Domain.Enums;
using LimsProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimsProjectTests.Application;

public class RollupServiceTests
{
    private static AppDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options);

    // ── Sem lotes ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SemLotes_NenhumSummaryEhCriado()
    {
        await using var db = CreateDb(nameof(SemLotes_NenhumSummaryEhCriado));
        var service = new RollupService(db);

        await service.ConsolidateDataAsync(CancellationToken.None);

        db.BatchesDailySummaries.Should().BeEmpty();
    }

    // ── Sem leituras hoje ──────────────────────────────────────────────────────

    [Fact]
    public async Task LoteSemSensorDataHoje_NenhumSummaryCriado()
    {
        await using var db = CreateDb(nameof(LoteSemSensorDataHoje_NenhumSummaryCriado));

        var batch = new Batch { Strain = "Mint", Status = BatchStatus.Growth };
        db.Batches.Add(batch);

        // Leitura de ontem — não deve entrar no cálculo de hoje
        db.SensorData.Add(new SensorData
        {
            BatchId = batch.Id,
            Temperature = 25m,
            ReadingTime = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var service = new RollupService(db);
        await service.ConsolidateDataAsync(CancellationToken.None);

        db.BatchesDailySummaries.Should().BeEmpty();
    }

    // ── Cria summary ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LoteComSensorDataHoje_CriaSummaryComMediaCorreta()
    {
        await using var db = CreateDb(nameof(LoteComSensorDataHoje_CriaSummaryComMediaCorreta));

        var batch = new Batch { Strain = "White Widow", Status = BatchStatus.Growth };
        db.Batches.Add(batch);
        db.SensorData.AddRange(
            new SensorData { BatchId = batch.Id, Temperature = 20m, ReadingTime = DateTime.UtcNow },
            new SensorData { BatchId = batch.Id, Temperature = 30m, ReadingTime = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var service = new RollupService(db);
        await service.ConsolidateDataAsync(CancellationToken.None);

        var summary = await db.BatchesDailySummaries.SingleAsync();
        summary.BatchId.Should().Be(batch.Id);
        summary.AvgTemperature.Should().Be(25m);
        summary.Date.Should().Be(DateTime.UtcNow.Date);
    }

    // ── Atualiza summary existente ─────────────────────────────────────────────

    [Fact]
    public async Task SummaryExistenteHoje_AtualizaMedia()
    {
        await using var db = CreateDb(nameof(SummaryExistenteHoje_AtualizaMedia));

        var batch = new Batch { Strain = "Dill", Status = BatchStatus.Growth };
        db.Batches.Add(batch);

        var existingSummary = new BatchDailySummary
        {
            BatchId = batch.Id,
            AvgTemperature = 10m,
            Date = DateTime.UtcNow.Date
        };
        db.BatchesDailySummaries.Add(existingSummary);

        db.SensorData.Add(new SensorData
        {
            BatchId = batch.Id,
            Temperature = 28m,
            ReadingTime = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new RollupService(db);
        await service.ConsolidateDataAsync(CancellationToken.None);

        var summaries = await db.BatchesDailySummaries.ToListAsync();
        summaries.Should().HaveCount(1);
        summaries[0].AvgTemperature.Should().Be(28m);
    }

    // ── Múltiplos lotes ────────────────────────────────────────────────────────

    [Fact]
    public async Task MultiplosLotes_CriaUmSummaryPorLote()
    {
        await using var db = CreateDb(nameof(MultiplosLotes_CriaUmSummaryPorLote));

        var batch1 = new Batch { Strain = "Mint" };
        var batch2 = new Batch { Strain = "Dill" };
        db.Batches.AddRange(batch1, batch2);

        db.SensorData.AddRange(
            new SensorData { BatchId = batch1.Id, Temperature = 22m, ReadingTime = DateTime.UtcNow },
            new SensorData { BatchId = batch2.Id, Temperature = 18m, ReadingTime = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var service = new RollupService(db);
        await service.ConsolidateDataAsync(CancellationToken.None);

        var summaries = await db.BatchesDailySummaries.ToListAsync();
        summaries.Should().HaveCount(2);
        summaries.Should().Contain(s => s.BatchId == batch1.Id && s.AvgTemperature == 22m);
        summaries.Should().Contain(s => s.BatchId == batch2.Id && s.AvgTemperature == 18m);
    }
}
