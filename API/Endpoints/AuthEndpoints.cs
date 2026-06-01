using LimsProject.Application.Interfaces;
using LimsProject.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;

namespace LimsProject.API.Endpoints;

public static class AuthEndpoints
{
    private static readonly string[] ValidRoles = ["Lab", "Admin"];

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
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

        app.MapPost("/auth/login", async (LoginRequest req, IAuthService auth, HttpContext ctx) =>
        {
            var tokens = await auth.LoginAsync(req.Email, req.Password);
            if (tokens is null)
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication failed",
                    detail: "Credenciais inválidas.");

            // Cookie HttpOnly carrega o refresh — frontend nem precisa armazenar
            AuthCookies.SetRefreshToken(ctx.Response, tokens.RefreshToken, tokens.RefreshTokenExpiresAt);
            return Results.Ok(tokens);
        }).AllowAnonymous().RequireRateLimiting("login");

        // Refresh aceita token via cookie HttpOnly (preferido) OU body (compat com clientes antigos / testes)
        app.MapPost("/auth/refresh", async (
            HttpContext ctx,
            IAuthService auth,
            RefreshRequest? body) =>
        {
            var refreshToken = body?.RefreshToken
                ?? ctx.Request.Cookies[AuthCookies.RefreshTokenName];

            if (string.IsNullOrWhiteSpace(refreshToken))
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Token refresh failed",
                    detail: "Refresh token não fornecido (esperado em cookie HttpOnly ou body).");

            var tokens = await auth.RefreshAsync(refreshToken);
            if (tokens is null)
            {
                AuthCookies.ClearRefreshToken(ctx.Response); // limpa cookie inválido
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Token refresh failed",
                    detail: "Refresh token inválido, expirado ou revogado.");
            }

            // Rotation: novo cookie substitui o antigo
            AuthCookies.SetRefreshToken(ctx.Response, tokens.RefreshToken, tokens.RefreshTokenExpiresAt);
            return Results.Ok(tokens);
        }).AllowAnonymous().RequireRateLimiting("login");

        app.MapPost("/auth/logout", async (
            HttpContext ctx,
            IAuthService auth,
            RefreshRequest? body) =>
        {
            var refreshToken = body?.RefreshToken
                ?? ctx.Request.Cookies[AuthCookies.RefreshTokenName];

            if (!string.IsNullOrWhiteSpace(refreshToken))
                await auth.RevokeAsync(refreshToken);

            AuthCookies.ClearRefreshToken(ctx.Response);
            return Results.NoContent();
        }).AllowAnonymous();
    }
}

public record RegisterRequest(string Email, string Password, string Role);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string? RefreshToken = null);
