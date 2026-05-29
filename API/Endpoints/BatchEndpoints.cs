using FluentValidation;
using LimsProject.Application.Interfaces;
using LimsProject.Application.Models;
using LimsProject.Domain.Entities;
using LimsProject.Domain.Enums;
using Microsoft.EntityFrameworkCore;

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

    public static void MapBatchEndpoints(this WebApplication app)
    {
        app.MapPost("/batches", async (Batch batch, ILimsDbContext db, IValidator<Batch> validator) =>
        {
            var result = await validator.ValidateAsync(batch);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            db.Batches.Add(batch);
            await db.SaveChangesAsync();
            return Results.Created($"/batches/{batch.Id}", batch);
        }).RequireAuthorization("AdminOnly");

        app.MapGet("/batches", async (
            ILimsDbContext db,
            int page = 1,
            int pageSize = 20,
            string? strain = null,
            BatchStatus? status = null) =>
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(1, page);

            var query = db.Batches.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(strain))
                query = query.Where(b => b.Strain.ToLower().Contains(strain.ToLower()));

            if (status.HasValue)
                query = query.Where(b => b.Status == status.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new PagedResult<Batch>(items, page, pageSize, total));
        }).RequireAuthorization();

        app.MapGet("/batches/{id}/summary", async (Guid id, ILimsDbContext db) =>
        {
            var batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == id);
            return batch is null ? Results.NotFound("Lote não encontrado.") : Results.Ok(batch);
        }).RequireAuthorization();

        app.MapPatch("/batches/{id}/status", async (Guid id, StatusUpdateRequest req, ILimsDbContext db) =>
        {
            var batch = await db.Batches.FindAsync(id);
            if (batch is null) return Results.NotFound("Lote não encontrado.");

            if (!ValidTransitions.TryGetValue(batch.Status, out var allowed) || !allowed.Contains(req.Status))
            {
                var permitted = allowed?.Length > 0 ? string.Join(", ", allowed) : "nenhuma";
                return Results.UnprocessableEntity(
                    $"Transição inválida: {batch.Status} → {req.Status}. Permitido: {permitted}.");
            }

            batch.Status = req.Status;
            await db.SaveChangesAsync();
            return Results.Ok(batch);
        }).RequireAuthorization("AdminOnly");

        app.MapDelete("/batches/{id}", async (Guid id, ILimsDbContext db) =>
        {
            var batch = await db.Batches.FindAsync(id);
            if (batch is null) return Results.NotFound("Lote não encontrado.");

            if (batch.Status is BatchStatus.Released or BatchStatus.Testing)
                return Results.Conflict($"Não é possível excluir um lote com status '{batch.Status}'.");

            db.Batches.Remove(batch);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");
    }
}

public record StatusUpdateRequest(BatchStatus Status);
