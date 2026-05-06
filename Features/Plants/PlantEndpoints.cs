using FluentValidation;

namespace LimsProject.Features.Plants;

public static class PlantEndpoints
{
    public static IEndpointRouteBuilder MapPlantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/batches/{batchId:guid}/plants", async (
            Guid batchId,
            RegisterPlantRequest body,
            IPlantService svc,
            IValidator<RegisterPlantRequest> validator,
            CancellationToken ct) =>
        {
            var vr = await validator.ValidateAsync(body, ct);
            if (!vr.IsValid)
                return Results.ValidationProblem(vr.ToDictionary());

            var result = await svc.RegisterAsync(batchId, body, ct);
            return result.IsSuccess
                ? Results.Created($"/plants/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        app.MapPatch("/plants/{plantId:guid}", async (
            Guid plantId,
            UpdatePlantRequest body,
            IPlantService svc,
            CancellationToken ct) =>
        {
            var result = await svc.UpdateAsync(plantId, body, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        return app;
    }
}
