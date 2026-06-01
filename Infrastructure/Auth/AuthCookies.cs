using Microsoft.AspNetCore.Http;

namespace LimsProject.Infrastructure.Auth;

/// <summary>
/// Gerencia o cookie HttpOnly que carrega o refresh token. JavaScript NÃO consegue ler
/// esse cookie — proteção contra XSS. O navegador anexa automaticamente nas requests
/// pra rotas sob Path "/auth".
/// </summary>
public static class AuthCookies
{
    public const string RefreshTokenName = "lims_refresh";

    public static void SetRefreshToken(HttpResponse response, string token, DateTime expiresAt)
    {
        response.Cookies.Append(RefreshTokenName, token, new CookieOptions
        {
            HttpOnly = true,            // JS não acessa
            Secure = true,              // só sobre HTTPS em prod; em dev (localhost) browser permite
            SameSite = SameSiteMode.Strict, // não anexa em requests cross-site (CSRF defense)
            Path = "/auth",             // só sobe nas chamadas de auth
            Expires = expiresAt,
            IsEssential = true,
        });
    }

    public static void ClearRefreshToken(HttpResponse response)
    {
        response.Cookies.Delete(RefreshTokenName, new CookieOptions
        {
            Path = "/auth",
            Secure = true,
            SameSite = SameSiteMode.Strict,
        });
    }
}
