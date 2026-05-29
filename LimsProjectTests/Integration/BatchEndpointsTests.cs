using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace LimsProjectTests.Integration;

public class BatchEndpointsTests(LimsWebApplicationFactory factory)
    : IClassFixture<LimsWebApplicationFactory>
{
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
        var adminClient = await factory.CreateAuthenticatedClientAsync("Admin");
        var created = await adminClient.PostAsJsonAsync("/batches", new { strain = "Purple Basil" });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var response = await adminClient.GetAsync($"/batches/{id}/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
