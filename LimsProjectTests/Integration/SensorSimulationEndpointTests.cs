using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace LimsProjectTests.Integration;

public class SensorSimulationEndpointTests(LimsWebApplicationFactory factory)
    : IClassFixture<LimsWebApplicationFactory>
{
    private async Task<(HttpClient labClient, HttpClient adminClient, Guid batchId)> SetupAsync()
    {
        var adminClient = await factory.CreateAuthenticatedClientAsync("Admin");
        var labClient = await factory.CreateAuthenticatedClientAsync("Lab");
        var resp = await adminClient.PostAsJsonAsync("/batches", new { strain = "Simulation Test Strain" });
        var id = (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        return (labClient, adminClient, id);
    }

    [Fact]
    public async Task POST_Simulate_Retorna202_ComBatchValido()
    {
        var (labClient, _, id) = await SetupAsync();

        var response = await labClient.PostAsync($"/batches/{id}/sensor-data/simulate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("batchId").GetGuid().Should().Be(id);
        body.GetProperty("durationSeconds").GetInt32().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task POST_Simulate_Retorna409_SeJaEstaRodando()
    {
        var (labClient, _, id) = await SetupAsync();

        var first = await labClient.PostAsync($"/batches/{id}/sensor-data/simulate", null);
        var second = await labClient.PostAsync($"/batches/{id}/sensor-data/simulate", null);

        first.StatusCode.Should().Be(HttpStatusCode.Accepted);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task POST_Simulate_Retorna404_LoteInexistente()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Lab");

        var response = await client.PostAsync($"/batches/{Guid.NewGuid()}/sensor-data/simulate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Simulate_Retorna409_LoteEmEstadoTerminal()
    {
        var (_, adminClient, id) = await SetupAsync();

        await adminClient.PostAsJsonAsync($"/batches/{id}/analysis", new
        {
            thc = 15.0,
            cbd = 1.0,
            terpenes = "0.5",
            isPassed = false,
        });

        var response = await adminClient.PostAsync($"/batches/{id}/sensor-data/simulate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task POST_Simulate_Retorna401_SemToken()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync($"/batches/{Guid.NewGuid()}/sensor-data/simulate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
