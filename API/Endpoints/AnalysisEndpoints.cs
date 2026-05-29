using FluentValidation;
using LimsProject.Application.Interfaces;
using LimsProject.Domain.Entities;
using LimsProject.Domain.Enums;

namespace LimsProject.API.Endpoints;

public static class AnalysisEndpoints
{
    public static void MapAnalysisEndpoints(this WebApplication app)
    {
        app.MapPost("/batches/{id}/analysis", async (
            Guid id,
            LabAnalysis analysis,
            ILimsDbContext db,
            IValidator<LabAnalysis> validator) =>
        {
            analysis.BatchId = id;
            analysis.AnalysisDate = DateTime.UtcNow;

            var validationResult = await validator.ValidateAsync(analysis);
            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            var batch = await db.Batches.FindAsync(id);
            if (batch is null) return Results.NotFound("Lote não encontrado.");

            db.LabAnalyses.Add(analysis);
            batch.Status = analysis.IsPassed ? BatchStatus.Released : BatchStatus.Rejected;

            await db.SaveChangesAsync();
            return Results.Created($"/analysis/{analysis.Id}", analysis);
        }).RequireAuthorization("LabOrAdmin");
    }
}
