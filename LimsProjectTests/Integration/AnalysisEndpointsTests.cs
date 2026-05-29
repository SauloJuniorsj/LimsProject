using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace LimsProjectTests.Integration;

public class AnalysisEndpointsTests(LimsWebApplicationFactory factory)
    : IClassFixture<LimsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<Guid> CriarLoteAsync(string strain = "Dill")
    {
        var response = await _client.PostAsJsonAsync("/batches", new { strain });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task POST_Analysis_Retorna404_QuandoLoteNaoExiste()
    {
        var payload = new { thc = 0.2, cbd = 5.0, terpenes = "citrus", isPassed = true };

        var response = await _client.PostAsJsonAsync($"/batches/{Guid.NewGuid()}/analysis", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Analysis_Retorna400_QuandoTHCNegativo()
    {
        var id = await CriarLoteAsync();
        var payload = new { thc = -1.0, cbd = 5.0, terpenes = "citrus", isPassed = false };

        var response = await _client.PostAsJsonAsync($"/batches/{id}/analysis", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Analysis_Retorna400_QuandoHempComplianceFalhar()
    {
        var id = await CriarLoteAsync();
        // THC > 0.3 e IsPassed = true viola a regra de cânhamo
        var payload = new { thc = 0.8, cbd = 2.0, terpenes = "earthy", isPassed = true };

        var response = await _client.PostAsJsonAsync($"/batches/{id}/analysis", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Analysis_Retorna201_EAlteraStatusParaReleased_QuandoAprovado()
    {
        var id = await CriarLoteAsync();
        var payload = new { thc = 0.2, cbd = 8.0, terpenes = "citrus", isPassed = true };

        var response = await _client.PostAsJsonAsync($"/batches/{id}/analysis", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verifica que o lote foi para Released (status = 4)
        var summary = await _client.GetFromJsonAsync<JsonElement>($"/batches/{id}/summary");
        summary.GetProperty("status").GetInt32().Should().Be(4); // BatchStatus.Released
    }

    [Fact]
    public async Task POST_Analysis_Retorna201_EAlteraStatusParaRejected_QuandoReprovado()
    {
        var id = await CriarLoteAsync();
        var payload = new { thc = 0.2, cbd = 1.0, terpenes = "skunky", isPassed = false };

        var response = await _client.PostAsJsonAsync($"/batches/{id}/analysis", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var summary = await _client.GetFromJsonAsync<JsonElement>($"/batches/{id}/summary");
        summary.GetProperty("status").GetInt32().Should().Be(5); // BatchStatus.Rejected
    }
}
