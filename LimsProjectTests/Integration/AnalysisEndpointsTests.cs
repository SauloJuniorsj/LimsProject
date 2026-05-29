using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace LimsProjectTests.Integration;

public class AnalysisEndpointsTests(LimsWebApplicationFactory factory)
    : IClassFixture<LimsWebApplicationFactory>
{
    private async Task<Guid> CriarLoteAsync(HttpClient adminClient, string strain = "Dill")
    {
        var response = await adminClient.PostAsJsonAsync("/batches", new { strain });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task POST_Analysis_Retorna401_SemToken()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/batches/{Guid.NewGuid()}/analysis",
            new { thc = 0.2, cbd = 5.0, terpenes = "citrus", isPassed = true });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_Analysis_Retorna404_QuandoLoteNaoExiste()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Lab");
        var payload = new { thc = 0.2, cbd = 5.0, terpenes = "citrus", isPassed = true };

        var response = await client.PostAsJsonAsync($"/batches/{Guid.NewGuid()}/analysis", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Analysis_Retorna400_QuandoTHCNegativo()
    {
        var adminClient = await factory.CreateAuthenticatedClientAsync("Admin");
        var labClient = await factory.CreateAuthenticatedClientAsync("Lab");
        var id = await CriarLoteAsync(adminClient);

        var response = await labClient.PostAsJsonAsync($"/batches/{id}/analysis",
            new { thc = -1.0, cbd = 5.0, terpenes = "citrus", isPassed = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Analysis_Retorna400_QuandoHempComplianceFalhar()
    {
        var adminClient = await factory.CreateAuthenticatedClientAsync("Admin");
        var labClient = await factory.CreateAuthenticatedClientAsync("Lab");
        var id = await CriarLoteAsync(adminClient);

        // THC > 0.3 e IsPassed = true viola a regra de cânhamo
        var response = await labClient.PostAsJsonAsync($"/batches/{id}/analysis",
            new { thc = 0.8, cbd = 2.0, terpenes = "earthy", isPassed = true });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Analysis_Retorna201_EAlteraStatusParaReleased_QuandoAprovado()
    {
        var adminClient = await factory.CreateAuthenticatedClientAsync("Admin");
        var labClient = await factory.CreateAuthenticatedClientAsync("Lab");
        var id = await CriarLoteAsync(adminClient);

        var response = await labClient.PostAsJsonAsync($"/batches/{id}/analysis",
            new { thc = 0.2, cbd = 8.0, terpenes = "citrus", isPassed = true });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var summary = await adminClient.GetFromJsonAsync<JsonElement>($"/batches/{id}/summary");
        summary.GetProperty("status").GetInt32().Should().Be(4); // BatchStatus.Released
    }

    [Fact]
    public async Task POST_Analysis_Retorna201_EAlteraStatusParaRejected_QuandoReprovado()
    {
        var adminClient = await factory.CreateAuthenticatedClientAsync("Admin");
        var labClient = await factory.CreateAuthenticatedClientAsync("Lab");
        var id = await CriarLoteAsync(adminClient);

        var response = await labClient.PostAsJsonAsync($"/batches/{id}/analysis",
            new { thc = 0.2, cbd = 1.0, terpenes = "skunky", isPassed = false });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var summary = await adminClient.GetFromJsonAsync<JsonElement>($"/batches/{id}/summary");
        summary.GetProperty("status").GetInt32().Should().Be(5); // BatchStatus.Rejected
    }
}
