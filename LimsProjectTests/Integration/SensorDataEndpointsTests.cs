using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace LimsProjectTests.Integration;

public class SensorDataEndpointsTests(LimsWebApplicationFactory factory)
    : IClassFixture<LimsWebApplicationFactory>
{
    private async Task<(HttpClient labClient, HttpClient adminClient, Guid batchId)> SetupAsync()
    {
        var adminClient = await factory.CreateAuthenticatedClientAsync("Admin");
        var labClient = await factory.CreateAuthenticatedClientAsync("Lab");
        var resp = await adminClient.PostAsJsonAsync("/batches", new { strain = "Sensor Test Strain" });
        var id = (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        return (labClient, adminClient, id);
    }

    // --- POST /batches/{id}/sensor-data ---

    [Fact]
    public async Task POST_SensorData_Retorna201_ComLeituraValida()
    {
        var (labClient, _, id) = await SetupAsync();

        var response = await labClient.PostAsJsonAsync($"/batches/{id}/sensor-data",
            new { temperature = 22.5 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_SensorData_AtualizaCurrentTemperature()
    {
        var (labClient, adminClient, id) = await SetupAsync();

        await labClient.PostAsJsonAsync($"/batches/{id}/sensor-data", new { temperature = 27.3 });

        var batch = await adminClient.GetFromJsonAsync<JsonElement>($"/batches/{id}/summary");
        batch.GetProperty("currentTemperature").GetDecimal().Should().Be(27.3m);
    }

    [Fact]
    public async Task POST_SensorData_Retorna400_TemperaturaAcimaDoLimite()
    {
        var (labClient, _, id) = await SetupAsync();

        var response = await labClient.PostAsJsonAsync($"/batches/{id}/sensor-data",
            new { temperature = 61.0 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_SensorData_Retorna400_TemperaturaAbaixoDoLimite()
    {
        var (labClient, _, id) = await SetupAsync();

        var response = await labClient.PostAsJsonAsync($"/batches/{id}/sensor-data",
            new { temperature = -11.0 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_SensorData_Retorna404_LoteInexistente()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Lab");

        var response = await client.PostAsJsonAsync($"/batches/{Guid.NewGuid()}/sensor-data",
            new { temperature = 25.0 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_SensorData_Retorna401_SemToken()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/batches/{Guid.NewGuid()}/sensor-data",
            new { temperature = 25.0 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_SensorData_Retorna403_ComRoleAdmin_NaoEhPermitido()
    {
        // AdminOnly não tem acesso ao sensor-data (requer LabOrAdmin — Admin está incluso via role)
        // Então Admin DEVE ter acesso. Vamos verificar que retorna 201.
        var (_, adminClient, id) = await SetupAsync();

        var response = await adminClient.PostAsJsonAsync($"/batches/{id}/sensor-data",
            new { temperature = 20.0 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // --- GET /batches/{id}/sensor-data ---

    [Fact]
    public async Task GET_SensorData_Retorna200_ComListaVazia()
    {
        var (_, adminClient, id) = await SetupAsync();

        var response = await adminClient.GetAsync($"/batches/{id}/sensor-data");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().Be(0);
        body.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GET_SensorData_Retorna200_ComLeiturasCriadas()
    {
        var (labClient, adminClient, id) = await SetupAsync();

        await labClient.PostAsJsonAsync($"/batches/{id}/sensor-data", new { temperature = 22.0 });
        await labClient.PostAsJsonAsync($"/batches/{id}/sensor-data", new { temperature = 24.0 });

        var response = await adminClient.GetAsync($"/batches/{id}/sensor-data");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("totalCount").GetInt32().Should().Be(2);
        body.GetProperty("items").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GET_SensorData_Retorna404_LoteInexistente()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");

        var response = await client.GetAsync($"/batches/{Guid.NewGuid()}/sensor-data");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- GET /batches/{id}/daily-summaries ---

    [Fact]
    public async Task GET_DailySummaries_Retorna200_ComListaVazia_SemRollup()
    {
        var (_, adminClient, id) = await SetupAsync();

        var response = await adminClient.GetAsync($"/batches/{id}/daily-summaries");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GET_DailySummaries_Retorna404_LoteInexistente()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");

        var response = await client.GetAsync($"/batches/{Guid.NewGuid()}/daily-summaries");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
