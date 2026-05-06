namespace LimsProject.Features.PostHarvest;

public static class PostHarvestEndpoints
{
    public static IEndpointRouteBuilder MapPostHarvestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/harvests/{harvestId:guid}/drying", async (Guid harvestId, IPostHarvestService svc, CancellationToken ct) =>
        {
            var result = await svc.StartDryingAsync(harvestId, ct);
            return result.IsSuccess
                ? Results.Created($"/drying/{result.Value}", new { DryingId = result.Value })
                : Results.BadRequest(result.Error);
        });

        app.MapPatch("/drying/{dryingId:guid}/complete", async (
            Guid dryingId,
            CompleteDryingRequest body,
            IPostHarvestService svc,
            CancellationToken ct) =>
        {
            var result = await svc.CompleteDryingAsync(dryingId, body, ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        app.MapPost("/drying/{dryingId:guid}/curing", async (Guid dryingId, IPostHarvestService svc, CancellationToken ct) =>
        {
            var result = await svc.StartCuringAsync(dryingId, ct);
            return result.IsSuccess
                ? Results.Created($"/curing/{result.Value}", new { CuringId = result.Value })
                : Results.BadRequest(result.Error);
        });

        app.MapPatch("/curing/{curingId:guid}/complete", async (
            Guid curingId,
            CompleteCuringRequest body,
            IPostHarvestService svc,
            CancellationToken ct) =>
        {
            var result = await svc.CompleteCuringAsync(curingId, body, ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        return app;
    }
}
