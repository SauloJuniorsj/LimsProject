using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LimsProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LimsProjectTests.Integration;

public class SoftDeleteAndAuditTests(LimsWebApplicationFactory factory)
    : IClassFixture<LimsWebApplicationFactory>
{
    private async Task<(HttpClient client, Guid id)> CriarLoteAsync(string strain)
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        var resp = await client.PostAsJsonAsync("/batches", new { strain });
        var id = (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        return (client, id);
    }

    // ── Audit fields: CreatedAt + CreatedBy ───────────────────────────────────

    [Fact]
    public async Task POST_Batches_PreencheCreatedAt_E_CreatedBy_AutomaticamenteViaInterceptor()
    {
        var strain = $"AuditCreate_{Guid.NewGuid():N}";
        var (_, id) = await CriarLoteAsync(strain);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var batch = await db.Batches.FirstAsync(b => b.Id == id);

        batch.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        batch.CreatedBy.Should().NotBeNullOrEmpty();
        batch.CreatedBy.Should().Contain("@test.com");
        batch.UpdatedAt.Should().BeNull();
        batch.UpdatedBy.Should().BeNull();
    }

    // ── Audit fields: UpdatedAt + UpdatedBy ───────────────────────────────────

    [Fact]
    public async Task PATCH_Batches_PreencheUpdatedAt_E_UpdatedBy()
    {
        var (client, id) = await CriarLoteAsync($"AuditUpdate_{Guid.NewGuid():N}");

        await client.PatchAsJsonAsync($"/batches/{id}/status", new { status = 1 }); // Germination → Growth

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var batch = await db.Batches.FirstAsync(b => b.Id == id);

        batch.UpdatedAt.Should().NotBeNull();
        batch.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        batch.UpdatedBy.Should().NotBeNullOrEmpty();
        batch.UpdatedBy.Should().Contain("@test.com");
    }

    // ── Soft delete: registro persiste fisicamente, DeletedAt setado ──────────

    [Fact]
    public async Task DELETE_Batch_PreservaRegistroNoDB_ComDeletedAt_E_DeletedBy()
    {
        var (client, id) = await CriarLoteAsync($"SoftDel_{Guid.NewGuid():N}");

        var delResp = await client.DeleteAsync($"/batches/{id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // IgnoreQueryFilters pra enxergar o soft-deleted
        var deleted = await db.Batches.IgnoreQueryFilters().FirstAsync(b => b.Id == id);

        deleted.DeletedAt.Should().NotBeNull();
        deleted.DeletedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        deleted.DeletedBy.Should().NotBeNullOrEmpty();
        deleted.DeletedBy.Should().Contain("@test.com");
    }

    // ── Global query filter: GET nem enxerga o soft-deleted ───────────────────

    [Fact]
    public async Task GET_Summary_Retorna404_AposSoftDelete_PorCausaDoQueryFilter()
    {
        var (client, id) = await CriarLoteAsync($"InvisibleAfterDel_{Guid.NewGuid():N}");
        await client.DeleteAsync($"/batches/{id}");

        var getResp = await client.GetAsync($"/batches/{id}/summary");

        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Batches_NaoListaSoftDeleted()
    {
        var strain = $"HiddenList_{Guid.NewGuid():N}";
        var (client, id) = await CriarLoteAsync(strain);

        await client.DeleteAsync($"/batches/{id}");

        var listResp = await client.GetAsync($"/batches?strain={strain}");
        var body = await listResp.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task DELETE_Batch_AposSoftDelete_Retorna404_NaSegundaTentativa()
    {
        var (client, id) = await CriarLoteAsync($"DoubleDel_{Guid.NewGuid():N}");

        var first = await client.DeleteAsync($"/batches/{id}");
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var second = await client.DeleteAsync($"/batches/{id}");
        second.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Sanity check: outras entidades NÃO são afetadas ──────────────────────

    [Fact]
    public async Task OutrasEntidades_NaoTemAuditFields_NaoSaoTocadasPeloInterceptor()
    {
        // RefreshToken não implementa IAuditable nem ISoftDeletable.
        // O interceptor usa ChangeTracker.Entries<IAuditable>() — type-safe, ignora.
        var client = factory.CreateClient();
        var email = $"audittest_{Guid.NewGuid():N}@test.com";
        const string password = "Test@1234";
        await client.PostAsJsonAsync("/auth/register", new { email, password, role = "Admin" });
        var resp = await client.PostAsJsonAsync("/auth/login", new { email, password });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Se o interceptor tentasse converter Deleted->Modified em RefreshToken,
        // o logout não conseguiria revogar — verificamos que o logout funciona normalmente.
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var refresh = body.GetProperty("refreshToken").GetString()!;
        var logoutResp = await client.PostAsJsonAsync("/auth/logout", new { refreshToken = refresh });
        logoutResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
