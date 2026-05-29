using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace LimsProjectTests.Integration;

public class StatusHistoryTests(LimsWebApplicationFactory factory)
    : IClassFixture<LimsWebApplicationFactory>
{
    private async Task<(HttpClient client, Guid id)> CriarLoteAsync(string strain = "HistoryStrain")
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        var resp = await client.PostAsJsonAsync("/batches", new { strain });
        var id = (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        return (client, id);
    }

    [Fact]
    public async Task POST_Batches_CriaEntradaInicialNoHistorico()
    {
        var (client, id) = await CriarLoteAsync();

        var response = await client.GetAsync($"/batches/{id}/status-history");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetArrayLength().Should().Be(1);

        var entry = body[0];
        entry.GetProperty("fromStatus").ValueKind.Should().Be(JsonValueKind.Null);
        entry.GetProperty("toStatus").GetInt32().Should().Be(0); // Germination
        entry.GetProperty("changedBy").GetString().Should().Contain("@test.com");
        entry.GetProperty("reason").GetString().Should().Be("Lote criado");
    }

    [Fact]
    public async Task PATCH_Status_AdicionaEntradaNoHistorico_ComReason()
    {
        var (client, id) = await CriarLoteAsync();

        await client.PatchAsJsonAsync($"/batches/{id}/status",
            new { status = 1, reason = "Transferido para vegetativo" });

        var history = await client.GetFromJsonAsync<JsonElement>($"/batches/{id}/status-history");
        history.GetArrayLength().Should().Be(2);

        // Ordenado por ChangedAt DESC: a entrada do PATCH vem primeiro
        var latest = history[0];
        latest.GetProperty("fromStatus").GetInt32().Should().Be(0);
        latest.GetProperty("toStatus").GetInt32().Should().Be(1);
        latest.GetProperty("reason").GetString().Should().Be("Transferido para vegetativo");
    }

    [Fact]
    public async Task PATCH_Status_AceitaSemReason()
    {
        var (client, id) = await CriarLoteAsync();

        var response = await client.PatchAsJsonAsync($"/batches/{id}/status", new { status = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await client.GetFromJsonAsync<JsonElement>($"/batches/{id}/status-history");
        history[0].GetProperty("reason").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task POST_Analysis_AdicionaEntradaNoHistorico_ComMudancaDeStatus()
    {
        var (admin, id) = await CriarLoteAsync();
        var lab = await factory.CreateAuthenticatedClientAsync("Lab");

        await lab.PostAsJsonAsync($"/batches/{id}/analysis",
            new { thc = 0.2, cbd = 5.0, terpenes = "citrus", isPassed = true });

        var history = await admin.GetFromJsonAsync<JsonElement>($"/batches/{id}/status-history");
        history.GetArrayLength().Should().Be(2);

        var latest = history[0];
        latest.GetProperty("fromStatus").GetInt32().Should().Be(0); // Germination → Released
        latest.GetProperty("toStatus").GetInt32().Should().Be(4);   // Released
        latest.GetProperty("reason").GetString().Should().Contain("Aprovado");
    }

    [Fact]
    public async Task GET_StatusHistory_Retorna404_QuandoLoteNaoExiste()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.GetAsync($"/batches/{Guid.NewGuid()}/status-history");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_StatusHistory_Retorna401_SemToken()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/batches/{Guid.NewGuid()}/status-history");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Historico_PreservaOrdemCronologica_AposMultiplasTransicoes()
    {
        var (client, id) = await CriarLoteAsync();

        // Germination → Growth → Harvested → Testing
        await client.PatchAsJsonAsync($"/batches/{id}/status", new { status = 1 });
        await client.PatchAsJsonAsync($"/batches/{id}/status", new { status = 2 });
        await client.PatchAsJsonAsync($"/batches/{id}/status", new { status = 3 });

        var history = await client.GetFromJsonAsync<JsonElement>($"/batches/{id}/status-history");
        history.GetArrayLength().Should().Be(4);

        // Ordem DESC: mais recente primeiro
        history[0].GetProperty("toStatus").GetInt32().Should().Be(3); // Testing
        history[1].GetProperty("toStatus").GetInt32().Should().Be(2); // Harvested
        history[2].GetProperty("toStatus").GetInt32().Should().Be(1); // Growth
        history[3].GetProperty("toStatus").GetInt32().Should().Be(0); // Germination (criação)
    }
}
