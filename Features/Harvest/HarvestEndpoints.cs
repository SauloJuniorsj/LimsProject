using FluentValidation;

namespace LimsProject.Features.Harvest;

public static class HarvestEndpoints
{
    public static IEndpointRouteBuilder MapHarvestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/batches/{batchId:guid}/harvest", async (
            Guid batchId,
            RegisterHarvestRequest body,
            IHarvestService svc,
            IValidator<RegisterHarvestRequest> validator,
            CancellationToken ct) =>
        {
            var vr = await validator.ValidateAsync(body, ct);
            if (!vr.IsValid)
                return Results.ValidationProblem(vr.ToDictionary());

            var result = await svc.RegisterAsync(batchId, body, ct);
            return result.IsSuccess
                ? Results.Created($"/harvests/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        return app;
    }
}
