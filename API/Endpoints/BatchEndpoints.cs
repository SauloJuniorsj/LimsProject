using System.Security.Claims;
using FluentValidation;
using LimsProject.API;
using LimsProject.Application.Caching;
using LimsProject.Application.Events;
using LimsProject.Application.Interfaces;
using LimsProject.Application.Models;
using LimsProject.Application.Observability;
using LimsProject.Application.Services;
using LimsProject.Domain.Entities;
using LimsProject.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LimsProject.API.Endpoints;

public static class BatchEndpoints
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

    public static void MapBatchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/batches", async (
            Batch batch,
            ILimsDbContext db,
            IValidator<Batch> validator,
            ClaimsPrincipal user,
            LimsMetrics metrics,
            IEventPublisher events) =>
        {
            var result = await validator.ValidateAsync(batch);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            db.Batches.Add(batch);
            StatusHistoryRecorder.Record(db, batch.Id, null, batch.Status, user, "Lote criado");
            await events.PublishAsync(new BatchCreatedEvent(batch.Id, batch.Strain, DateTime.UtcNow));
            await db.SaveChangesAsync();
            metrics.BatchCreated();
            return Results.Created($"/batches/{batch.Id}", batch);
        }).RequireAuthorization("AdminOnly");

        app.MapGet("/batches", async (
            ILimsDbContext db,
            int page = 1,
            int pageSize = 20,
            string? strain = null,
            BatchStatus? status = null,
            string? sortBy = null,
            string? sortDir = null,
            DateTime? createdAfter = null,
            DateTime? createdBefore = null) =>
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(1, page);

            var query = db.Batches.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(strain))
                query = query.Where(b => b.Strain.ToLower().Contains(strain.ToLower()));

            if (status.HasValue)
                query = query.Where(b => b.Status == status.Value);

            if (createdAfter.HasValue)
                query = query.Where(b => b.CreatedAt >= createdAfter.Value);

            if (createdBefore.HasValue)
                query = query.Where(b => b.CreatedAt <= createdBefore.Value);

            // Sort whitelist explícita — evita SQL injection via param e dá erro claro
            var ascending = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
            query = (sortBy?.ToLowerInvariant()) switch
            {
                "strain"  => ascending ? query.OrderBy(b => b.Strain)    : query.OrderByDescending(b => b.Strain),
                "status"  => ascending ? query.OrderBy(b => b.Status)    : query.OrderByDescending(b => b.Status),
                "createdat" or null or "" =>
                             ascending ? query.OrderBy(b => b.CreatedAt) : query.OrderByDescending(b => b.CreatedAt),
                _ => query.OrderByDescending(b => b.CreatedAt)
            };

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new PagedResult<Batch>(items, page, pageSize, total));
        }).RequireAuthorization();

        app.MapGet("/batches/{id}/summary", async (Guid id, ILimsDbContext db, IMemoryCache cache) =>
        {
            var key = CacheKeys.BatchSummary(id);
            var batch = await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                entry.SlidingExpiration = TimeSpan.FromSeconds(10);
                return await db.Batches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            });
            return batch is null ? Problems.BatchNotFound() : Results.Ok(batch);
        }).RequireAuthorization();

        app.MapPatch("/batches/{id}/status", async (
            Guid id,
            StatusUpdateRequest req,
            ILimsDbContext db,
            ClaimsPrincipal user,
            LimsMetrics metrics,
            IEventPublisher events,
            IMemoryCache cache) =>
        {
            var batch = await db.Batches.FindAsync(id);
            if (batch is null) return Problems.BatchNotFound();

            if (!ValidTransitions.TryGetValue(batch.Status, out var allowed) || !allowed.Contains(req.Status))
                return Problems.InvalidStatusTransition(batch.Status, req.Status, allowed ?? []);

            var from = batch.Status;
            batch.Status = req.Status;
            StatusHistoryRecorder.Record(db, batch.Id, from, req.Status, user, req.Reason);
            await events.PublishAsync(new BatchStatusChangedEvent(
                batch.Id, from, req.Status,
                user.FindFirstValue(System.Security.Claims.ClaimTypes.Email) ?? "anonymous",
                req.Reason, DateTime.UtcNow));
            await db.SaveChangesAsync();
            metrics.StatusTransition(from.ToString(), req.Status.ToString());
            cache.Remove(CacheKeys.BatchSummary(id));
            return Results.Ok(batch);
        }).RequireAuthorization("AdminOnly");

        app.MapDelete("/batches/{id}", async (Guid id, ILimsDbContext db, IMemoryCache cache) =>
        {
            var batch = await db.Batches.FindAsync(id);
            if (batch is null) return Problems.BatchNotFound();

            if (batch.Status is BatchStatus.Released or BatchStatus.Testing)
                return Problems.CannotDeleteBatch(batch.Status);

            db.Batches.Remove(batch);
            await db.SaveChangesAsync();
            cache.Remove(CacheKeys.BatchSummary(id));
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        app.MapGet("/batches/{id}/daily-summaries", async (
            Guid id,
            ILimsDbContext db,
            DateTime? from = null,
            DateTime? to = null) =>
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
        }).RequireAuthorization();

        app.MapGet("/batches/{id}/status-history", async (Guid id, ILimsDbContext db) =>
        {
            var exists = await db.Batches.AsNoTracking().AnyAsync(b => b.Id == id);
            if (!exists) return Problems.BatchNotFound();

            var history = await db.BatchStatusHistories
                .AsNoTracking()
                .Where(h => h.BatchId == id)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();

            return Results.Ok(history);
        }).RequireAuthorization();

        app.MapGet("/batches/{id}/certificate-of-analysis", async (Guid id, ILimsDbContext db) =>
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

            var environmental = summaries.Count == 0
                ? new EnvironmentalConditions(0, null, null, null, 0)
                : new EnvironmentalConditions(
                    DaysMonitored: summaries.Count,
                    OverallAvgTemperature: summaries.Average(s => s.AvgTemperature),
                    OverallMinTemperature: summaries.Min(s => s.MinTemperature),
                    OverallMaxTemperature: summaries.Max(s => s.MaxTemperature),
                    TotalReadings: summaries.Sum(s => s.ReadingCount));

            var passing = analyses.Where(a => a.IsPassed).ToList();
            var compliance = new ComplianceSummary(
                HasPassingAnalysis: passing.Count > 0,
                HempCompliant: passing.Count == 0 || passing.All(a => a.THC <= 0.3m),
                AnalysisCount: analyses.Count,
                LastAnalysisDate: analyses.FirstOrDefault()?.AnalysisDate);

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
        }).RequireAuthorization();
    }
}

public record StatusUpdateRequest(BatchStatus Status, string? Reason = null);
