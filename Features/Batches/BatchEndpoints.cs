using FluentValidation;

namespace LimsProject.Features.Batches;

public static class BatchEndpoints
{
    public static IEndpointRouteBuilder MapBatchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/batches", async (
            CreateBatchRequest body,
            IBatchService svc,
            IValidator<CreateBatchRequest> validator,
            CancellationToken ct) =>
        {
            var vr = await validator.ValidateAsync(body, ct);
            if (!vr.IsValid)
                return Results.ValidationProblem(vr.ToDictionary());

            var result = await svc.CreateAsync(body, ct);
            return result.IsSuccess
                ? Results.Created($"/batches/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        app.MapGet("/batches/{id:guid}/summary", async (Guid id, IBatchService svc, CancellationToken ct) =>
        {
            var s = await svc.GetSummaryAsync(id, ct);
            return s is null ? Results.NotFound() : Results.Ok(s);
        });

        app.MapPost("/batches/{id:guid}/transition", async (
            Guid id,
            TransitionBatchRequest body,
            IBatchService svc,
            IValidator<TransitionBatchRequest> validator,
            CancellationToken ct) =>
        {
            var vr = await validator.ValidateAsync(body, ct);
            if (!vr.IsValid)
                return Results.ValidationProblem(vr.ToDictionary());

            var result = await svc.TransitionAsync(id, body, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        return app;
    }
}
