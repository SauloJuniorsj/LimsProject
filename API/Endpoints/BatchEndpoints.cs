using LimsProject.Application.Interfaces;
using LimsProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.API.Endpoints;

public static class BatchEndpoints
{
    public static void MapBatchEndpoints(this WebApplication app)
    {
        app.MapPost("/batches", async (Batch batch, ILimsDbContext db) =>
        {
            db.Batches.Add(batch);
            await db.SaveChangesAsync();
            return Results.Created($"/batches/{batch.Id}", batch);
        }).RequireAuthorization("AdminOnly");

        app.MapGet("/batches/{id}/summary", async (Guid id, ILimsDbContext db) =>
        {
            var batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == id);
            return batch is null ? Results.NotFound("Lote não encontrado.") : Results.Ok(batch);
        }).RequireAuthorization();
    }
}
