using LimsProject.Application.Models;

namespace LimsProject.Application.Interfaces;

public record AuthResult(bool Succeeded, IEnumerable<string> Errors);

public interface IAuthService
{
    Task<AuthTokens?> LoginAsync(string email, string password);
    Task<AuthResult> RegisterAsync(string email, string password, string role);
    Task<AuthTokens?> RefreshAsync(string refreshToken);
    Task<bool> RevokeAsync(string refreshToken);
}
