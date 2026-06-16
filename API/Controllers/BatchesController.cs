using System.Security.Claims;
using Asp.Versioning;
using FluentValidation;
using LimsProject.Application.Caching;
using LimsProject.Application.Events;
using LimsProject.Application.Interfaces;
using LimsProject.Application.Models;
using LimsProject.Application.Observability;
using LimsProject.Application.Services;
using LimsProject.Domain.Entities;
using LimsProject.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LimsProject.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("batches")]
[Authorize]
public class BatchesController(
    ILimsDbContext db,
    IMemoryCache cache,
    IValidator<Batch> validator,
    LimsMetrics metrics,
    IEventPublisher events) : ControllerBase
{
    private static readonly Dictionary<BatchStatus, BatchStatus[]> ValidTransitions = new()
    {
        [BatchStatus.Germination] = [BatchStatus.Growth],
        [BatchStatus.Growth]      = [BatchStatus.Harvested],
        [BatchStatus.Harvested]   = [BatchStatus.Testing],
        [BatchStatus.Testing]     = [BatchStatus.Released, BatchStatus.Rejected],
        [BatchStatus.Released]    = [],
        [BatchStatus.Rejected]    = [],
    };

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IResult> Post([FromBody] BatchCreateRequest req)
    {
        var batch = new Batch { Strain = req.Strain };

        var result = await validator.ValidateAsync(batch);
        if (!result.IsValid)
            return Results.ValidationProblem(result.ToDictionary());

        db.Batches.Add(batch);
        StatusHistoryRecorder.Record(db, batch.Id, null, batch.Status, User, "Lote criado");
        await events.PublishAsync(new BatchCreatedEvent(batch.Id, batch.Strain, DateTime.UtcNow));
        await db.SaveChangesAsync();
        metrics.BatchCreated();
        return Results.Created($"/batches/{batch.Id}", batch);
    }

    [HttpGet]
    public async Task<IResult> List([FromQuery] BatchListQuery q)
    {
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        var page = Math.Max(1, q.Page);

        var query = ApplySorting(ApplyFilters(db.Batches.AsNoTracking(), q), q.SortBy, q.SortDir);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Results.Ok(new PagedResult<Batch>(items, page, pageSize, total));
    }

    private static IQueryable<Batch> ApplyFilters(IQueryable<Batch> query, BatchListQuery q)
    {
        if (!string.IsNullOrWhiteSpace(q.Strain))
            query = query.Where(b => b.Strain.ToLower().Contains(q.Strain.ToLower()));

        if (q.Status.HasValue)
            query = query.Where(b => b.Status == q.Status.Value);

        if (q.CreatedAfter.HasValue)
            query = query.Where(b => b.CreatedAt >= q.CreatedAfter.Value);

        if (q.CreatedBefore.HasValue)
            query = query.Where(b => b.CreatedAt <= q.CreatedBefore.Value);

        return query;
    }

    private static readonly Dictionary<string, Func<IQueryable<Batch>, bool, IOrderedQueryable<Batch>>> Sorters =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["strain"]    = (q, asc) => asc ? q.OrderBy(b => b.Strain)    : q.OrderByDescending(b => b.Strain),
            ["status"]    = (q, asc) => asc ? q.OrderBy(b => b.Status)    : q.OrderByDescending(b => b.Status),
            ["createdat"] = (q, asc) => asc ? q.OrderBy(b => b.CreatedAt) : q.OrderByDescending(b => b.CreatedAt),
        };

    private static IQueryable<Batch> ApplySorting(IQueryable<Batch> query, string? sortBy, string? sortDir)
    {
        var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        var key = string.IsNullOrWhiteSpace(sortBy) ? "createdat" : sortBy;
        return Sorters.TryGetValue(key, out var sorter)
            ? sorter(query, asc)
            : query.OrderByDescending(b => b.CreatedAt);
    }

    [HttpGet("{id:guid}/summary")]
    public async Task<IResult> GetSummary(Guid id)
    {
        var key = CacheKeys.BatchSummary(id);
        var batch = await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            entry.SlidingExpiration = TimeSpan.FromSeconds(10);
            return await db.Batches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        });
        return batch is null ? Problems.BatchNotFound() : Results.Ok(batch);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IResult> ChangeStatus(Guid id, [FromBody] StatusUpdateRequest req)
    {
        var batch = await db.Batches.FindAsync(id);
        if (batch is null) return Problems.BatchNotFound();

        if (!ValidTransitions.TryGetValue(batch.Status, out var allowed) || !allowed.Contains(req.Status))
            return Problems.InvalidStatusTransition(batch.Status, req.Status, allowed ?? []);

        var from = batch.Status;
        batch.Status = req.Status;
        StatusHistoryRecorder.Record(db, batch.Id, from, req.Status, User, req.Reason);
        await events.PublishAsync(new BatchStatusChangedEvent(
            batch.Id, from, req.Status,
            User.FindFirstValue(ClaimTypes.Email) ?? "anonymous",
            req.Reason, DateTime.UtcNow));
        await db.SaveChangesAsync();
        metrics.StatusTransition(from.ToString(), req.Status.ToString());
        cache.Remove(CacheKeys.BatchSummary(id));
        return Results.Ok(batch);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IResult> Delete(Guid id)
    {
        var batch = await db.Batches.FindAsync(id);
        if (batch is null) return Problems.BatchNotFound();

        if (batch.Status is BatchStatus.Released or BatchStatus.Testing)
            return Problems.CannotDeleteBatch(batch.Status);

        db.Batches.Remove(batch);
        await db.SaveChangesAsync();
        cache.Remove(CacheKeys.BatchSummary(id));
        return Results.NoContent();
    }

    [HttpGet("{id:guid}/daily-summaries")]
    public async Task<IResult> GetDailySummaries(Guid id, DateTime? from = null, DateTime? to = null)
    {
        var exists = await db.Batches.AsNoTracking().AnyAsync(b => b.Id == id);
        if (!exists) return Problems.BatchNotFound();

        var query = db.BatchesDailySummaries
            .AsNoTracking()
            .Where(s => s.BatchId == id);

        if (from.HasValue)
            query = query.Where(s => s.Date >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(s => s.Date <= to.Value.Date);

        var summaries = await query
            .OrderByDescending(s => s.Date)
            .ToListAsync();

        return Results.Ok(summaries);
    }

    [HttpGet("{id:guid}/status-history")]
    public async Task<IResult> GetStatusHistory(Guid id)
    {
        var exists = await db.Batches.AsNoTracking().AnyAsync(b => b.Id == id);
        if (!exists) return Problems.BatchNotFound();

        var history = await db.BatchStatusHistories
            .AsNoTracking()
            .Where(h => h.BatchId == id)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();

        return Results.Ok(history);
    }

    [HttpGet("{id:guid}/certificate-of-analysis")]
    public async Task<IResult> GetCertificateOfAnalysis(Guid id)
    {
        var batch = await db.Batches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (batch is null) return Problems.BatchNotFound();

        var analyses = await db.LabAnalyses.AsNoTracking()
            .Where(a => a.BatchId == id)
            .OrderByDescending(a => a.AnalysisDate)
            .ToListAsync();

        var summaries = await db.BatchesDailySummaries.AsNoTracking()
            .Where(s => s.BatchId == id)
            .ToListAsync();

        var lifecycle = await db.BatchStatusHistories.AsNoTracking()
            .Where(h => h.BatchId == id)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

        var environmental = BuildEnvironmental(summaries);
        var compliance = BuildCompliance(analyses);

        var coa = new CertificateOfAnalysis(
            BatchId: batch.Id,
            Strain: batch.Strain,
            Status: batch.Status,
            BatchCreatedAt: batch.CreatedAt,
            Analyses: analyses,
            Environmental: environmental,
            Lifecycle: lifecycle,
            Compliance: compliance,
            IssuedAt: DateTime.UtcNow);

        return Results.Ok(coa);
    }

    private static EnvironmentalConditions BuildEnvironmental(List<BatchDailySummary> summaries) =>
        summaries.Count == 0
            ? new EnvironmentalConditions(0, null, null, null, 0)
            : new EnvironmentalConditions(
                DaysMonitored: summaries.Count,
                OverallAvgTemperature: summaries.Average(s => s.AvgTemperature),
                OverallMinTemperature: summaries.Min(s => s.MinTemperature),
                OverallMaxTemperature: summaries.Max(s => s.MaxTemperature),
                TotalReadings: summaries.Sum(s => s.ReadingCount));

    private static ComplianceSummary BuildCompliance(List<LabAnalysis> analyses)
    {
        var passing = analyses.Where(a => a.IsPassed).ToList();
        return new ComplianceSummary(
            HasPassingAnalysis: passing.Count > 0,
            HempCompliant: passing.Count == 0 || passing.All(a => a.THC <= 0.3m),
            AnalysisCount: analyses.Count,
            LastAnalysisDate: analyses.FirstOrDefault()?.AnalysisDate);
    }
}

public record BatchCreateRequest(string Strain);

public record BatchListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Strain = null,
    BatchStatus? Status = null,
    string? SortBy = null,
    string? SortDir = null,
    DateTime? CreatedAfter = null,
    DateTime? CreatedBefore = null);

public record StatusUpdateRequest(
    [property: System.Text.Json.Serialization.JsonRequired] BatchStatus Status,
    string? Reason = null);
