namespace LimsProject.Application.Interfaces;

/// <summary>
/// Abstração da identidade do usuário atual sem acoplar Application ao HttpContext.
/// Retorna null quando não há request (background workers, migrations, etc).
/// </summary>
public interface ICurrentUserService
{
    string? GetEmail();
}
