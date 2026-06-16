using System.Security.Claims;
using Asp.Versioning;
using FluentValidation;
using LimsProject.Application.Events;
using LimsProject.Application.Interfaces;
using LimsProject.Application.Observability;
using LimsProject.Application.Services;
using LimsProject.Domain.Entities;
using LimsProject.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("batches/{id:guid}")]
[Authorize(Policy = "LabOrAdmin")]
public class AnalysisController(
    ILimsDbContext db,
    IValidator<LabAnalysis> validator,
    LimsMetrics metrics,
    IEventPublisher events) : ControllerBase
{
    [HttpPost("analysis")]
    public async Task<IResult> Post(Guid id, [FromBody] LabAnalysisCreateRequest req)
    {
        var analysis = new LabAnalysis
        {
            BatchId = id,
            AnalysisDate = DateTime.UtcNow,
            THC = req.THC,
            CBD = req.CBD,
            Terpenes = req.Terpenes,
            IsPassed = req.IsPassed,
        };

        var validationResult = await validator.ValidateAsync(analysis);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var batch = await db.Batches.FindAsync(id);
        if (batch is null) return Problems.BatchNotFound();

        db.LabAnalyses.Add(analysis);

        var newStatus = analysis.IsPassed ? BatchStatus.Released : BatchStatus.Rejected;
        BatchStatus? previousStatus = null;
        if (newStatus != batch.Status)
        {
            previousStatus = batch.Status;
            StatusHistoryRecorder.Record(db, batch.Id, batch.Status, newStatus, User,
                analysis.IsPassed ? "Aprovado em análise laboratorial" : "Reprovado em análise laboratorial");
            batch.Status = newStatus;
        }

        await events.PublishAsync(new AnalysisCompletedEvent(
            batch.Id, analysis.Id, analysis.THC, analysis.CBD, analysis.IsPassed, DateTime.UtcNow));
        if (previousStatus.HasValue)
        {
            await events.PublishAsync(new BatchStatusChangedEvent(
                batch.Id, previousStatus, newStatus,
                User.FindFirstValue(ClaimTypes.Email) ?? "anonymous",
                "Mudança automática via análise laboratorial",
                DateTime.UtcNow));
        }

        await db.SaveChangesAsync();
        metrics.AnalysisCompleted(analysis.IsPassed);

        return Results.Created($"/batches/{id}/analyses/{analysis.Id}", analysis);
    }

    [HttpGet("analyses")]
    public async Task<IResult> List(Guid id)
    {
        var exists = await db.Batches.AsNoTracking().AnyAsync(b => b.Id == id);
        if (!exists) return Problems.BatchNotFound();

        var analyses = await db.LabAnalyses
            .AsNoTracking()
            .Where(a => a.BatchId == id)
            .OrderByDescending(a => a.AnalysisDate)
            .ToListAsync();

        return Results.Ok(analyses);
    }
}

public record LabAnalysisCreateRequest(
    [property: System.Text.Json.Serialization.JsonRequired] decimal THC,
    [property: System.Text.Json.Serialization.JsonRequired] decimal CBD,
    string Terpenes,
    [property: System.Text.Json.Serialization.JsonRequired] bool IsPassed);
