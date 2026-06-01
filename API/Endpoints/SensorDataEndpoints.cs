using FluentValidation;
using LimsProject.API;
using LimsProject.Application.Interfaces;
using LimsProject.Application.Models;
using LimsProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.API.Endpoints;

public static class SensorDataEndpoints
{
    public static void MapSensorDataEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/batches/{id}/sensor-data", async (
            Guid id,
            SensorReading req,
            ILimsDbContext db,
            IValidator<SensorReading> validator) =>
        {
            var result = await validator.ValidateAsync(req);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            var batch = await db.Batches.FindAsync(id);
            if (batch is null) return Problems.BatchNotFound();

            var reading = new SensorData
            {
                BatchId = id,
                Temperature = req.Temperature,
                ReadingTime = DateTime.UtcNow
            };

            db.SensorData.Add(reading);
            batch.CurrentTemperature = req.Temperature;
            await db.SaveChangesAsync();

            return Results.Created($"/batches/{id}/sensor-data/{reading.Id}", reading);
        }).RequireAuthorization("LabOrAdmin");

        app.MapGet("/batches/{id}/sensor-data", async (
            Guid id,
            ILimsDbContext db,
            int page = 1,
            int pageSize = 50) =>
        {
            var exists = await db.Batches.AsNoTracking().AnyAsync(b => b.Id == id);
            if (!exists) return Problems.BatchNotFound();

            pageSize = Math.Clamp(pageSize, 1, 200);
            page = Math.Max(1, page);

            var total = await db.SensorData.CountAsync(s => s.BatchId == id);
            var items = await db.SensorData
                .AsNoTracking()
                .Where(s => s.BatchId == id)
                .OrderByDescending(s => s.ReadingTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new PagedResult<SensorData>(items, page, pageSize, total));
        }).RequireAuthorization();
    }
}
