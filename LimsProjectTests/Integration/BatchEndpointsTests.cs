using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace LimsProjectTests.Integration;

public class BatchEndpointsTests(LimsWebApplicationFactory factory)
    : IClassFixture<LimsWebApplicationFactory>
{
    private async Task<(HttpClient adminClient, Guid batchId)> CriarLoteAsync(string strain = "White Widow")
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        var resp = await client.PostAsJsonAsync("/batches", new { strain });
        var id = (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        return (client, id);
    }

    // --- POST /batches ---

    [Fact]
    public async Task POST_Batches_Retorna201_ComLoteValido()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.PostAsJsonAsync("/batches", new { strain = "White Widow" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_Batches_RetornaLoteCriado_NoBody()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.PostAsJsonAsync("/batches", new { strain = "Mint" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("strain").GetString().Should().Be("Mint");
        body.GetProperty("id").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task POST_Batches_Retorna400_QuandoStrainVazio()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.PostAsJsonAsync("/batches", new { strain = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Batches_Retorna401_SemToken()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/batches", new { strain = "Dill" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_Batches_Retorna403_ComRoleLab()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Lab");
        var response = await client.PostAsJsonAsync("/batches", new { strain = "Dill" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- GET /batches (lista paginada) ---

    [Fact]
    public async Task GET_Batches_Retorna200_ComPagedResult()
    {
        var (client, _) = await CriarLoteAsync("UniqueStrainForListTest");

        var response = await client.GetAsync("/batches?page=1&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
        body.GetProperty("page").GetInt32().Should().Be(1);
        body.GetProperty("pageSize").GetInt32().Should().Be(50);
        body.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(0);
        body.GetProperty("totalPages").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GET_Batches_FiltrarPorStrain_RetornaApenasMatchs()
    {
        var uniqueStrain = $"STrainFilter_{Guid.NewGuid():N}";
        var (client, _) = await CriarLoteAsync(uniqueStrain);

        var response = await client.GetAsync($"/batches?strain={uniqueStrain}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("totalCount").GetInt32().Should().Be(1);
        body.GetProperty("items")[0].GetProperty("strain").GetString().Should().Be(uniqueStrain);
    }

    [Fact]
    public async Task GET_Batches_FiltrarPorStatus_RetornaApenasMatchs()
    {
        var (client, _) = await CriarLoteAsync("StatusFilterBatch");

        // status=0 → Germination (default ao criar)
        var response = await client.GetAsync("/batches?status=0");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(0);
        foreach (var item in body.GetProperty("items").EnumerateArray())
            item.GetProperty("status").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GET_Batches_Retorna401_SemToken()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/batches");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- GET /batches/{id}/summary ---

    [Fact]
    public async Task GET_Summary_Retorna404_QuandoLoteNaoExiste()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync($"/batches/{Guid.NewGuid()}/summary");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Summary_Retorna200_QuandoLoteExiste()
    {
        var (client, id) = await CriarLoteAsync("Purple Basil");

        var response = await client.GetAsync($"/batches/{id}/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- PATCH /batches/{id}/status ---

    [Fact]
    public async Task PATCH_Status_Retorna200_ComTransicaoValida()
    {
        var (client, id) = await CriarLoteAsync("PatchTestBatch");

        var response = await client.PatchAsJsonAsync($"/batches/{id}/status", new { status = 1 }); // Growth

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task PATCH_Status_Retorna422_ComTransicaoInvalida()
    {
        var (client, id) = await CriarLoteAsync("InvalidTransitionBatch");

        // Germination → Harvested é inválido (pula Growth)
        var response = await client.PatchAsJsonAsync($"/batches/{id}/status", new { status = 2 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PATCH_Status_Retorna404_QuandoLoteNaoExiste()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.PatchAsJsonAsync($"/batches/{Guid.NewGuid()}/status", new { status = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- DELETE /batches/{id} ---

    [Fact]
    public async Task DELETE_Batch_Retorna204_QuandoExcluido()
    {
        var (client, id) = await CriarLoteAsync("DeleteMeBatch");

        var response = await client.DeleteAsync($"/batches/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_Batch_Retorna409_QuandoStatusEhTesting()
    {
        var (client, id) = await CriarLoteAsync("CannotDeleteBatch");

        // Germination → Growth → Harvested → Testing
        await client.PatchAsJsonAsync($"/batches/{id}/status", new { status = 1 });
        await client.PatchAsJsonAsync($"/batches/{id}/status", new { status = 2 });
        await client.PatchAsJsonAsync($"/batches/{id}/status", new { status = 3 });

        var response = await client.DeleteAsync($"/batches/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DELETE_Batch_Retorna404_QuandoNaoExiste()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.DeleteAsync($"/batches/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
