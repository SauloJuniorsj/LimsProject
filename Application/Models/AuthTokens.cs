namespace LimsProject.Application.Models;

public record AuthTokens(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt
);
