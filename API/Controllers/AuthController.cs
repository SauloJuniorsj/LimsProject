using Asp.Versioning;
using LimsProject.Application.Interfaces;
using LimsProject.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LimsProject.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("auth")]
[AllowAnonymous]
public class AuthController(IAuthService auth) : ControllerBase
{
    private static readonly string[] ValidRoles = ["Lab", "Admin"];

    [HttpPost("register")]
    public async Task<IResult> Register([FromBody] RegisterRequest req)
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
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IResult> Login([FromBody] LoginRequest req)
    {
        var tokens = await auth.LoginAsync(req.Email, req.Password);
        if (tokens is null)
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication failed",
                detail: "Credenciais inválidas.");

        AuthCookies.SetRefreshToken(HttpContext.Response, tokens.RefreshToken, tokens.RefreshTokenExpiresAt);
        return Results.Ok(tokens);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("login")]
    public async Task<IResult> Refresh([FromBody] RefreshRequest? body)
    {
        var refreshToken = body?.RefreshToken
            ?? HttpContext.Request.Cookies[AuthCookies.RefreshTokenName];

        if (string.IsNullOrWhiteSpace(refreshToken))
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token refresh failed",
                detail: "Refresh token não fornecido (esperado em cookie HttpOnly ou body).");

        var tokens = await auth.RefreshAsync(refreshToken);
        if (tokens is null)
        {
            AuthCookies.ClearRefreshToken(HttpContext.Response);
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token refresh failed",
                detail: "Refresh token inválido, expirado ou revogado.");
        }

        AuthCookies.SetRefreshToken(HttpContext.Response, tokens.RefreshToken, tokens.RefreshTokenExpiresAt);
        return Results.Ok(tokens);
    }

    [HttpPost("logout")]
    public async Task<IResult> Logout([FromBody] RefreshRequest? body)
    {
        var refreshToken = body?.RefreshToken
            ?? HttpContext.Request.Cookies[AuthCookies.RefreshTokenName];

        if (!string.IsNullOrWhiteSpace(refreshToken))
            await auth.RevokeAsync(refreshToken);

        AuthCookies.ClearRefreshToken(HttpContext.Response);
        return Results.NoContent();
    }
}

public record RegisterRequest(string Email, string Password, string Role);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string? RefreshToken = null);
