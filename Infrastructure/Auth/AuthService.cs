using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LimsProject.Application.Interfaces;
using LimsProject.Application.Models;
using LimsProject.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace LimsProject.Infrastructure.Auth;

public class AuthService(
    UserManager<IdentityUser> userManager,
    IConfiguration config,
    ILimsDbContext db,
    ILogger<AuthService> logger) : IAuthService
{
    private const int AccessTokenLifetimeHours = 1;
    private const int RefreshTokenLifetimeDays = 30;
    private const int RefreshTokenByteLength = 64;

    public async Task<AuthTokens?> LoginAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
            return null;

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string role)
    {
        var user = new IdentityUser { Email = email, UserName = email };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return new AuthResult(false, result.Errors.Select(e => e.Description));

        await userManager.AddToRoleAsync(user, role);
        return new AuthResult(true, []);
    }

    public async Task<AuthTokens?> RefreshAsync(string refreshToken)
    {
        var hash = HashToken(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (stored is null) return null;

        // Reuse detection (OWASP recommendation): apresentar um token já revogado
        // sinaliza possível roubo — revoga TODA a cadeia daquele usuário.
        if (stored.IsRevoked)
        {
            logger.LogWarning(
                "Refresh token reuse detected for user {UserId} (token {TokenId}). Revoking all tokens.",
                stored.UserId, stored.Id);
            await RevokeAllForUserAsync(stored.UserId);
            return null;
        }

        if (stored.IsExpired) return null;

        var user = await userManager.FindByIdAsync(stored.UserId);
        if (user is null) return null;

        // Rotation: emite novos tokens e marca o atual como substituído
        var newTokens = await IssueTokensAsync(user);
        var newStored = await db.RefreshTokens.FirstAsync(t => t.TokenHash == HashToken(newTokens!.RefreshToken));

        stored.RevokedAt = DateTime.UtcNow;
        stored.ReplacedByTokenId = newStored.Id;
        await db.SaveChangesAsync();

        return newTokens;
    }

    public async Task<bool> RevokeAsync(string refreshToken)
    {
        var hash = HashToken(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (stored is null || stored.IsRevoked) return false;

        stored.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<AuthTokens> IssueTokensAsync(IdentityUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, accessExpiresAt) = GenerateAccessToken(user, roles);
        var (refreshTokenValue, refreshExpiresAt) = await GenerateAndStoreRefreshTokenAsync(user.Id);
        return new AuthTokens(accessToken, refreshTokenValue, accessExpiresAt, refreshExpiresAt);
    }

    private (string token, DateTime expiresAt) GenerateAccessToken(IdentityUser user, IList<string> roles)
    {
        var expiresAt = DateTime.UtcNow.AddHours(AccessTokenLifetimeHours);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private async Task<(string token, DateTime expiresAt)> GenerateAndStoreRefreshTokenAsync(string userId)
    {
        var tokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(RefreshTokenByteLength));
        var expiresAt = DateTime.UtcNow.AddDays(RefreshTokenLifetimeDays);

        db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = HashToken(tokenValue),
            UserId = userId,
            ExpiresAt = expiresAt
        });
        await db.SaveChangesAsync();

        return (tokenValue, expiresAt);
    }

    private async Task RevokeAllForUserAsync(string userId)
    {
        var active = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var token in active) token.RevokedAt = now;
        await db.SaveChangesAsync();
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hash);
    }
}
