using LimsProject.Application.Interfaces;

namespace LimsProject.API.Endpoints;

public static class AuthEndpoints
{
    private static readonly string[] ValidRoles = ["Lab", "Admin"];

    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/register", async (RegisterRequest req, IAuthService auth) =>
        {
            if (!ValidRoles.Contains(req.Role))
                return Results.BadRequest($"Role inválida. Use: {string.Join(", ", ValidRoles)}.");

            var result = await auth.RegisterAsync(req.Email, req.Password, req.Role);
            return result.Succeeded
                ? Results.Created($"/auth/{req.Email}", null)
                : Results.BadRequest(result.Errors);
        }).AllowAnonymous();

        app.MapPost("/auth/login", async (LoginRequest req, IAuthService auth) =>
        {
            var token = await auth.LoginAsync(req.Email, req.Password);
            return token is null
                ? Results.Unauthorized()
                : Results.Ok(new { token });
        }).AllowAnonymous();
    }
}

public record RegisterRequest(string Email, string Password, string Role);
public record LoginRequest(string Email, string Password);
