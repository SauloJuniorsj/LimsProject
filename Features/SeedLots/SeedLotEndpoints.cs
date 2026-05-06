using FluentValidation;

namespace LimsProject.Features.SeedLots;

public static class SeedLotEndpoints
{
    public static IEndpointRouteBuilder MapSeedLotEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/seed-lots", async (
            CreateSeedLotRequest body,
            ISeedLotService svc,
            IValidator<CreateSeedLotRequest> validator,
            CancellationToken ct) =>
        {
            var vr = await validator.ValidateAsync(body, ct);
            if (!vr.IsValid)
                return Results.ValidationProblem(vr.ToDictionary());

            var result = await svc.CreateAsync(body, ct);
            return result.IsSuccess
                ? Results.Created($"/seed-lots/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        return app;
    }
}
