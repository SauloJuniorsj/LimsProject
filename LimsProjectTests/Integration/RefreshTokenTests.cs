using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LimsProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LimsProjectTests.Integration;

public class RefreshTokenTests(LimsWebApplicationFactory factory)
    : IClassFixture<LimsWebApplicationFactory>
{
    private async Task<(string email, string password)> RegisterUserAsync(HttpClient client, string role = "Admin")
    {
        var email = $"refresh_{role.ToLower()}_{Guid.NewGuid():N}@test.com";
        const string password = "Test@1234";
        await client.PostAsJsonAsync("/auth/register", new { email, password, role });
        return (email, password);
    }

    private async Task<JsonElement> LoginAsync(HttpClient client, string email, string password)
    {
        var resp = await client.PostAsJsonAsync("/auth/login", new { email, password });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        return await resp.Content.ReadFromJsonAsync<JsonElement>();
    }

    // ── Login emite o par de tokens ───────────────────────────────────────────

    [Fact]
    public async Task POST_Login_RetornaAccessToken_E_RefreshToken()
    {
        var client = factory.CreateClient();
        var (email, password) = await RegisterUserAsync(client);

        var body = await LoginAsync(client, email, password);

        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("accessTokenExpiresAt").GetDateTime().Should().BeAfter(DateTime.UtcNow);
        body.GetProperty("refreshTokenExpiresAt").GetDateTime().Should().BeAfter(DateTime.UtcNow.AddDays(29));
    }

    [Fact]
    public async Task POST_Login_RetornaProblemDetails_QuandoCredenciaisInvalidas()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login",
            new { email = "nonexistent@test.com", password = "wrong" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Authentication failed");
        body.GetProperty("status").GetInt32().Should().Be(401);
    }

    // ── Refresh flow ──────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_Refresh_RetornaNovosTokens_QuandoRefreshTokenValido()
    {
        var client = factory.CreateClient();
        var (email, password) = await RegisterUserAsync(client);
        var loginBody = await LoginAsync(client, email, password);
        var originalRefresh = loginBody.GetProperty("refreshToken").GetString()!;

        var refreshResp = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = originalRefresh });

        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshBody = await refreshResp.Content.ReadFromJsonAsync<JsonElement>();
        refreshBody.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        refreshBody.GetProperty("refreshToken").GetString().Should().NotBe(originalRefresh); // rotation!
    }

    [Fact]
    public async Task POST_Refresh_RetornaProblemDetails_QuandoTokenInvalido()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = "lixo-invalido" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Token refresh failed");
    }

    [Fact]
    public async Task POST_Refresh_ReuseDetection_RevogaCadeiaInteira_QuandoTokenRevogadoEhApresentado()
    {
        var client = factory.CreateClient();
        var (email, password) = await RegisterUserAsync(client);
        var loginBody = await LoginAsync(client, email, password);
        var originalRefresh = loginBody.GetProperty("refreshToken").GetString()!;

        // Refresh #1 → invalida o original, gera token B
        var firstRefreshResp = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = originalRefresh });
        var tokenB = (await firstRefreshResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("refreshToken").GetString()!;

        // Atacante apresenta o original (que já foi revogado) → reuse detection
        var reuseResp = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = originalRefresh });
        reuseResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Token B legítimo TAMBÉM deve estar revogado (defesa em profundidade)
        var subsequentResp = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = tokenB });
        subsequentResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_Refresh_TokenAntigoNaoFunciona_AposRotation()
    {
        var client = factory.CreateClient();
        var (email, password) = await RegisterUserAsync(client);
        var loginBody = await LoginAsync(client, email, password);
        var originalRefresh = loginBody.GetProperty("refreshToken").GetString()!;

        // Rotate uma vez
        await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = originalRefresh });

        // Apresentar de novo → falha
        var response = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = originalRefresh });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_Logout_Retorna204_E_InvalidaRefreshToken()
    {
        var client = factory.CreateClient();
        var (email, password) = await RegisterUserAsync(client);
        var loginBody = await LoginAsync(client, email, password);
        var refresh = loginBody.GetProperty("refreshToken").GetString()!;

        var logoutResp = await client.PostAsJsonAsync("/auth/logout", new { refreshToken = refresh });
        logoutResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Refresh com o token revogado falha
        var refreshResp = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = refresh });
        refreshResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_Logout_Retorna204_MesmoComTokenInexistente_Idempotente()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/logout", new { refreshToken = "qualquer-coisa" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Persistência: hash, não plaintext ─────────────────────────────────────

    [Fact]
    public async Task RefreshToken_EhPersistidoComoHash_NaoComoPlaintext()
    {
        var client = factory.CreateClient();
        var (email, password) = await RegisterUserAsync(client);
        var loginBody = await LoginAsync(client, email, password);
        var refresh = loginBody.GetProperty("refreshToken").GetString()!;

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Plaintext NÃO deve aparecer no DB
        var plaintextFound = await db.RefreshTokens.AnyAsync(t => t.TokenHash == refresh);
        plaintextFound.Should().BeFalse();

        // Mas existe UM token persistido com hash diferente
        var any = await db.RefreshTokens.AnyAsync();
        any.Should().BeTrue();
    }
}
