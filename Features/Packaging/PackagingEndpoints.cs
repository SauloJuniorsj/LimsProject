using FluentValidation;
using LimsProject.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Features.Packaging;

public static class PackagingEndpoints
{
    public static IEndpointRouteBuilder MapPackagingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/packages/{serial}", async (string serial, AppDbContext db, CancellationToken ct) =>
        {
            var pkg = await db.ProductPackages.AsNoTracking().FirstOrDefaultAsync(p => p.SerialNumber == serial, ct);
            if (pkg is null)
                return Results.NotFound();

            return Results.Ok(new ProductPackageResponse(
                pkg.Id,
                pkg.SerialNumber,
                $"lims:{pkg.SerialNumber}",
                pkg.WeightGrams,
                pkg.PackagedAt));
        });

        app.MapPost("/products", async (
            CreateFinishedProductRequest body,
            IPackagingService svc,
            IValidator<CreateFinishedProductRequest> validator,
            CancellationToken ct) =>
        {
            var vr = await validator.ValidateAsync(body, ct);
            if (!vr.IsValid)
                return Results.ValidationProblem(vr.ToDictionary());

            var result = await svc.CreateFinishedProductAsync(body, ct);
            return result.IsSuccess
                ? Results.Created($"/products/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        app.MapPost("/batches/{batchId:guid}/packages", async (
            Guid batchId,
            PackBatchRequest body,
            IPackagingService svc,
            IValidator<PackBatchRequest> validator,
            CancellationToken ct) =>
        {
            var vr = await validator.ValidateAsync(body, ct);
            if (!vr.IsValid)
                return Results.ValidationProblem(vr.ToDictionary());

            var result = await svc.PackBatchAsync(batchId, body, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        app.MapPost("/packages/{serial}/sell", async (
            string serial,
            IPackagingService svc,
            CancellationToken ct) =>
        {
            var result = await svc.MarkPackageSoldAsync(serial, ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        return app;
    }
}
