using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LimsProject.Application.Caching;
using LimsProject.Domain.Entities;
using LimsProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LimsProjectTests.Integration;

// ── Sorting & advanced filtering ──────────────────────────────────────────────

public class AdvancedQueryTests(LimsWebApplicationFactory factory)
    : IClassFixture<LimsWebApplicationFactory>
{
    [Fact]
    public async Task GET_Batches_OrdenaPorStrainAsc()
    {
        var prefix = $"Sort_{Guid.NewGuid():N}_";
        var client = await factory.CreateAuthenticatedClientAsync("Admin");

        await client.PostAsJsonAsync("/batches", new { strain = $"{prefix}Cherry" });
        await client.PostAsJsonAsync("/batches", new { strain = $"{prefix}Apple" });
        await client.PostAsJsonAsync("/batches", new { strain = $"{prefix}Banana" });

        var resp = await client.GetAsync($"/batches?strain={prefix}&sortBy=strain&sortDir=asc");
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var strains = body.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("strain").GetString()!).ToList();

        strains.Should().Equal($"{prefix}Apple", $"{prefix}Banana", $"{prefix}Cherry");
    }

    [Fact]
    public async Task GET_Batches_OrdenaPorStrainDesc()
    {
        var prefix = $"SortDesc_{Guid.NewGuid():N}_";
        var client = await factory.CreateAuthenticatedClientAsync("Admin");

        await client.PostAsJsonAsync("/batches", new { strain = $"{prefix}Apple" });
        await client.PostAsJsonAsync("/batches", new { strain = $"{prefix}Cherry" });
        await client.PostAsJsonAsync("/batches", new { strain = $"{prefix}Banana" });

        var resp = await client.GetAsync($"/batches?strain={prefix}&sortBy=strain&sortDir=desc");
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var strains = body.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("strain").GetString()!).ToList();

        strains.Should().Equal($"{prefix}Cherry", $"{prefix}Banana", $"{prefix}Apple");
    }

    [Fact]
    public async Task GET_Batches_FiltraPorCreatedAfter()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        var strain = $"DateFilter_{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/batches", new { strain });

        var future = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var past = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ");

        var noneResp = await client.GetAsync($"/batches?strain={strain}&createdAfter={future}");
        var noneBody = await noneResp.Content.ReadFromJsonAsync<JsonElement>();
        noneBody.GetProperty("totalCount").GetInt32().Should().Be(0);

        var foundResp = await client.GetAsync($"/batches?strain={strain}&createdAfter={past}");
        var foundBody = await foundResp.Content.ReadFromJsonAsync<JsonElement>();
        foundBody.GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GET_Batches_SortByInvalido_CaiNoFallback_PorCreatedAtDesc()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        // sortBy=lixo não deve quebrar — a whitelist do switch cai no default
        var resp = await client.GetAsync("/batches?sortBy=garbage&sortDir=garbage");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

// ── API versioning ────────────────────────────────────────────────────────────

public class ApiVersioningTests(LimsWebApplicationFactory factory)
    : IClassFixture<LimsWebApplicationFactory>
{
    [Fact]
    public async Task SemHeader_RetornaDefaultV1_NaResposta()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        var resp = await client.GetAsync("/batches");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Headers.Should().Contain(h => h.Key == "api-supported-versions");
        resp.Headers.GetValues("api-supported-versions").First().Should().Contain("1.0");
    }

    [Fact]
    public async Task ComQueryStringDeVersao_RequestAceito()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        var resp = await client.GetAsync("/batches?api-version=1.0");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

// ── Caching com invalidação targeted ──────────────────────────────────────────

public class CachingTests(LimsWebApplicationFactory factory)
    : IClassFixture<LimsWebApplicationFactory>
{
    [Fact]
    public async Task GET_Summary_PopulaCache_NaPrimeiraChamada()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        var resp = await client.PostAsJsonAsync("/batches", new { strain = "CacheStrain" });
        var id = (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Primeira chamada: cache miss, busca no DB
        await client.GetAsync($"/batches/{id}/summary");

        using var scope = factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        var key = CacheKeys.BatchSummary(id);

        cache.TryGetValue(key, out _).Should().BeTrue("a primeira leitura deve ter populado o cache");
    }

    [Fact]
    public async Task PATCH_Status_InvalidaCacheDoSummary()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        var resp = await client.PostAsJsonAsync("/batches", new { strain = "CacheInvalidate" });
        var id = (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await client.GetAsync($"/batches/{id}/summary"); // popula cache
        await client.PatchAsJsonAsync($"/batches/{id}/status", new { status = 1 });

        using var scope = factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        cache.TryGetValue(CacheKeys.BatchSummary(id), out _)
            .Should().BeFalse("PATCH deve ter removido a chave do cache");
    }

    [Fact]
    public async Task DELETE_InvalidaCacheDoSummary()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        var resp = await client.PostAsJsonAsync("/batches", new { strain = "CacheDel" });
        var id = (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await client.GetAsync($"/batches/{id}/summary");
        await client.DeleteAsync($"/batches/{id}");

        using var scope = factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        cache.TryGetValue(CacheKeys.BatchSummary(id), out _).Should().BeFalse();
    }
}

// ── Healthcheck do outbox lag ─────────────────────────────────────────────────

public class HealthCheckTests(LimsWebApplicationFactory factory)
    : IClassFixture<LimsWebApplicationFactory>
{
    private async Task PurgeOutboxAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.OutboxMessages.RemoveRange(db.OutboxMessages);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GET_Health_RetornaHealthy_SemOutboxLag()
    {
        await PurgeOutboxAsync();

        var client = factory.CreateClient();
        var resp = await client.GetAsync("/health");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OutboxLag_DeReporta_Unhealthy_Com10MensagensAtrasadas()
    {
        await PurgeOutboxAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var old = DateTime.UtcNow.AddMinutes(-5);
            for (int i = 0; i < 10; i++)
            {
                db.OutboxMessages.Add(new OutboxMessage
                {
                    EventType = "TestLagEvent",
                    RoutingKey = "lims.test",
                    Payload = "{}",
                    CreatedAt = old
                });
            }
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        var resp = await client.GetAsync("/health");

        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        await PurgeOutboxAsync(); // limpa pra não atrapalhar testes seguintes
    }
}
