using FluentValidation;
using LimsProject.Common.Persistence;
using LimsProject.Features.Sensors.Alerts;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Features.Sensors;

public static class SensorEndpoints
{
    public static IEndpointRouteBuilder MapSensorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/sensors/bulk", async (
            BulkSensorRequest body,
            ISensorIngestionService svc,
            IValidator<BulkSensorRequest> validator,
            CancellationToken ct) =>
        {
            var vr = await validator.ValidateAsync(body, ct);
            if (!vr.IsValid)
                return Results.ValidationProblem(vr.ToDictionary());

            var result = await svc.IngestBulkAsync(body, ct);
            return result.IsSuccess ? Results.Ok(new { Ingested = result.Value }) : Results.BadRequest(result.Error);
        });

        app.MapPost("/alert-thresholds", async (
            CreateThresholdRequest body,
            IAlertThresholdService svc,
            IValidator<CreateThresholdRequest> validator,
            CancellationToken ct) =>
        {
            var vr = await validator.ValidateAsync(body, ct);
            if (!vr.IsValid)
                return Results.ValidationProblem(vr.ToDictionary());

            var result = await svc.CreateAsync(body, ct);
            return result.IsSuccess
                ? Results.Created($"/alert-thresholds/{result.Value}", new { Id = result.Value })
                : Results.BadRequest(result.Error);
        });

        app.MapGet("/alerts", async (bool? openOnly, AppDbContext db, CancellationToken ct) =>
        {
            var q = db.EnvironmentalAlerts.AsNoTracking();
            if (openOnly == true)
                q = q.Where(a => !a.Resolved);

            var list = await q
                .OrderByDescending(a => a.CreatedAt)
                .Take(200)
                .Select(a => new AlertResponse(a.Id, a.BatchId, a.SensorDataId, a.Message, a.Resolved, a.CreatedAt))
                .ToListAsync(ct);

            return Results.Ok(list);
        });

        return app;
    }
}
