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

    // --- POST /batches/{id}/analysis ---

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

    // --- GET /batches/{id}/analyses ---

    [Fact]
    public async Task GET_Analyses_Retorna200_ComListaVazia_QuandoSemAnalises()
    {
        var adminClient = await factory.CreateAuthenticatedClientAsync("Admin");
        var labClient = await factory.CreateAuthenticatedClientAsync("Lab");
        var id = await CriarLoteAsync(adminClient, "EmptyAnalysesBatch");

        var response = await labClient.GetAsync($"/batches/{id}/analyses");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GET_Analyses_Retorna200_ComAnalisesCriadas()
    {
        var adminClient = await factory.CreateAuthenticatedClientAsync("Admin");
        var labClient = await factory.CreateAuthenticatedClientAsync("Lab");
        var id = await CriarLoteAsync(adminClient, "WithAnalysesBatch");

        await labClient.PostAsJsonAsync($"/batches/{id}/analysis",
            new { thc = 0.1, cbd = 3.0, terpenes = "pine", isPassed = true });

        var response = await labClient.GetAsync($"/batches/{id}/analyses");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetArrayLength().Should().Be(1);
        body[0].GetProperty("batchId").GetGuid().Should().Be(id);
    }

    [Fact]
    public async Task GET_Analyses_Retorna404_QuandoLoteNaoExiste()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Lab");
        var response = await client.GetAsync($"/batches/{Guid.NewGuid()}/analyses");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
