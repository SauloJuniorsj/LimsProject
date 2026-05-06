using FluentValidation;

namespace LimsProject.Features.LabAnalysis;

public static class LabAnalysisEndpoints
{
    public static IEndpointRouteBuilder MapLabAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/batches/{batchId:guid}/analysis", async (
            Guid batchId,
            CreateLabAnalysisRequest body,
            ILabAnalysisService svc,
            IValidator<CreateLabAnalysisRequest> validator,
            CancellationToken ct) =>
        {
            var vr = await validator.ValidateAsync(body, ct);
            if (!vr.IsValid)
                return Results.ValidationProblem(vr.ToDictionary());

            var result = await svc.SubmitAsync(batchId, body, ct);
            return result.IsSuccess
                ? Results.Created($"/analysis/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        return app;
    }
}
