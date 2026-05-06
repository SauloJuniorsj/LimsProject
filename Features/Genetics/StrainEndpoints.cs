using FluentValidation;

namespace LimsProject.Features.Genetics;

public static class StrainEndpoints
{
    public static IEndpointRouteBuilder MapStrainEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/strains", async (
            CreateStrainRequest body,
            IStrainService svc,
            IValidator<CreateStrainRequest> validator,
            CancellationToken ct) =>
        {
            var vr = await validator.ValidateAsync(body, ct);
            if (!vr.IsValid)
                return Results.ValidationProblem(vr.ToDictionary());

            var result = await svc.CreateAsync(body, ct);
            return result.IsSuccess
                ? Results.Created($"/strains/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        app.MapGet("/strains/{id:guid}", async (Guid id, IStrainService svc, CancellationToken ct) =>
        {
            var strain = await svc.GetByIdAsync(id, ct);
            return strain is null ? Results.NotFound() : Results.Ok(strain);
        });

        return app;
    }
}
