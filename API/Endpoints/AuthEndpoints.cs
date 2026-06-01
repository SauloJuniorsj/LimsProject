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
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid role",
                    detail: $"Role inválida. Use: {string.Join(", ", ValidRoles)}.");

            var result = await auth.RegisterAsync(req.Email, req.Password, req.Role);
            return result.Succeeded
                ? Results.Created($"/auth/{req.Email}", null)
                : Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Registration failed",
                    detail: string.Join("; ", result.Errors));
        }).AllowAnonymous();

        app.MapPost("/auth/login", async (LoginRequest req, IAuthService auth) =>
        {
            var tokens = await auth.LoginAsync(req.Email, req.Password);
            return tokens is null
                ? Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication failed",
                    detail: "Credenciais inválidas.")
                : Results.Ok(tokens);
        }).AllowAnonymous().RequireRateLimiting("login");

        app.MapPost("/auth/refresh", async (RefreshRequest req, IAuthService auth) =>
        {
            var tokens = await auth.RefreshAsync(req.RefreshToken);
            return tokens is null
                ? Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Token refresh failed",
                    detail: "Refresh token inválido, expirado ou revogado.")
                : Results.Ok(tokens);
        }).AllowAnonymous().RequireRateLimiting("login");

        app.MapPost("/auth/logout", async (RefreshRequest req, IAuthService auth) =>
        {
            await auth.RevokeAsync(req.RefreshToken);
            // Idempotent: 204 mesmo se o token não existir/já estiver revogado
            return Results.NoContent();
        }).AllowAnonymous();
    }
}

public record RegisterRequest(string Email, string Password, string Role);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
