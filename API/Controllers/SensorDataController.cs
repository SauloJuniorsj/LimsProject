using Asp.Versioning;
using FluentValidation;
using LimsProject.Application.Events;
using LimsProject.Application.Interfaces;
using LimsProject.Application.Models;
using LimsProject.Application.Services;
using LimsProject.Domain.Entities;
using LimsProject.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("batches/{id:guid}/sensor-data")]
[Authorize]
public class SensorDataController(
    ILimsDbContext db,
    IValidator<SensorReading> validator,
    IEventPublisher events,
    ISensorSimulationService simulation) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "LabOrAdmin")]
    public async Task<IResult> Post(Guid id, [FromBody] SensorReading req)
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
            ReadingTime = DateTime.UtcNow,
        };

        db.SensorData.Add(reading);
        batch.CurrentTemperature = req.Temperature;

        if (SensorThresholds.IsOutOfRange(req.Temperature))
        {
            await events.PublishAsync(new SensorReadingOutOfRangeEvent(
                BatchId: id,
                ReadingId: reading.Id,
                Temperature: req.Temperature,
                MinThreshold: SensorThresholds.MinTemperatureCelsius,
                MaxThreshold: SensorThresholds.MaxTemperatureCelsius,
                ReadingTime: reading.ReadingTime,
                OccurredAt: DateTime.UtcNow));
        }

        await db.SaveChangesAsync();

        return Results.Created($"/batches/{id}/sensor-data/{reading.Id}", new
        {
            reading.Id,
            reading.BatchId,
            reading.Temperature,
            reading.ReadingTime,
            outOfRange = SensorThresholds.IsOutOfRange(req.Temperature),
            thresholds = new
            {
                min = SensorThresholds.MinTemperatureCelsius,
                max = SensorThresholds.MaxTemperatureCelsius,
            },
        });
    }

    /// <summary>
    /// Dispara um burst de leituras sintéticas (background) pra demonstrar telemetria +
    /// RollupWorker ao vivo, sem exigir entrada manual. Retorna 202 e segue em background.
    /// </summary>
    [HttpPost("simulate")]
    [Authorize(Policy = "LabOrAdmin")]
    public async Task<IResult> Simulate(Guid id)
    {
        var batch = await db.Batches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (batch is null) return Problems.BatchNotFound();
        if (batch.Status is BatchStatus.Released or BatchStatus.Rejected)
            return Problems.BatchTerminal(batch.Status);

        if (!simulation.TryStart(id))
            return Problems.SimulationAlreadyRunning();

        return Results.Accepted(value: new
        {
            batchId = id,
            durationSeconds = simulation.TotalDurationSeconds,
            message = "Simulação iniciada — leituras chegando a cada poucos segundos.",
        });
    }

    [HttpGet]
    public async Task<IResult> List(Guid id, int page = 1, int pageSize = 50)
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
    }
}
