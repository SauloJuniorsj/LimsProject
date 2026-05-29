namespace LimsProject.Application.Interfaces;

public record AuthResult(bool Succeeded, IEnumerable<string> Errors);

public interface IAuthService
{
    Task<string?> LoginAsync(string email, string password);
    Task<AuthResult> RegisterAsync(string email, string password, string role);
}
