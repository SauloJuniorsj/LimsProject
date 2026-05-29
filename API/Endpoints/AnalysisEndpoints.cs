using System.Security.Claims;
using FluentValidation;
using LimsProject.Application.Events;
using LimsProject.Application.Interfaces;
using LimsProject.Application.Observability;
using LimsProject.Application.Services;
using LimsProject.Domain.Entities;
using LimsProject.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.API.Endpoints;

public static class AnalysisEndpoints
{
    public static void MapAnalysisEndpoints(this WebApplication app)
    {
        app.MapPost("/batches/{id}/analysis", async (
            Guid id,
            LabAnalysis analysis,
            ILimsDbContext db,
            IValidator<LabAnalysis> validator,
            ClaimsPrincipal user,
            LimsMetrics metrics,
            IEventPublisher events) =>
        {
            analysis.BatchId = id;
            analysis.AnalysisDate = DateTime.UtcNow;

            var validationResult = await validator.ValidateAsync(analysis);
            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            var batch = await db.Batches.FindAsync(id);
            if (batch is null) return Results.NotFound("Lote não encontrado.");

            db.LabAnalyses.Add(analysis);

            var newStatus = analysis.IsPassed ? BatchStatus.Released : BatchStatus.Rejected;
            BatchStatus? previousStatus = null;
            if (newStatus != batch.Status)
            {
                previousStatus = batch.Status;
                StatusHistoryRecorder.Record(db, batch.Id, batch.Status, newStatus, user,
                    analysis.IsPassed ? "Aprovado em análise laboratorial" : "Reprovado em análise laboratorial");
                batch.Status = newStatus;
            }

            await db.SaveChangesAsync();
            metrics.AnalysisCompleted(analysis.IsPassed);

            await events.PublishAsync(new AnalysisCompletedEvent(
                batch.Id, analysis.Id, analysis.THC, analysis.CBD, analysis.IsPassed, DateTime.UtcNow));
            if (previousStatus.HasValue)
            {
                await events.PublishAsync(new BatchStatusChangedEvent(
                    batch.Id, previousStatus, newStatus,
                    user.FindFirstValue(ClaimTypes.Email) ?? "anonymous",
                    "Mudança automática via análise laboratorial",
                    DateTime.UtcNow));
            }

            return Results.Created($"/batches/{id}/analyses/{analysis.Id}", analysis);
        }).RequireAuthorization("LabOrAdmin");

        app.MapGet("/batches/{id}/analyses", async (Guid id, ILimsDbContext db) =>
        {
            var exists = await db.Batches.AsNoTracking().AnyAsync(b => b.Id == id);
            if (!exists) return Results.NotFound("Lote não encontrado.");

            var analyses = await db.LabAnalyses
                .AsNoTracking()
                .Where(a => a.BatchId == id)
                .OrderByDescending(a => a.AnalysisDate)
                .ToListAsync();

            return Results.Ok(analyses);
        }).RequireAuthorization("LabOrAdmin");
    }
}
