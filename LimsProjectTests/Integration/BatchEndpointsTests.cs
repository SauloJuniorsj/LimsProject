using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace LimsProjectTests.Integration;

public class BatchEndpointsTests(LimsWebApplicationFactory factory)
    : IClassFixture<LimsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task POST_Batches_Retorna201_ComLoteValido()
    {
        var payload = new { strain = "White Widow" };

        var response = await _client.PostAsJsonAsync("/batches", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_Batches_RetornaLoteCriado_NoBody()
    {
        var payload = new { strain = "Mint" };

        var response = await _client.PostAsJsonAsync("/batches", payload);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("strain").GetString().Should().Be("Mint");
        body.GetProperty("id").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GET_Summary_Retorna404_QuandoLoteNaoExiste()
    {
        var response = await _client.GetAsync($"/batches/{Guid.NewGuid()}/summary");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Summary_Retorna200_QuandoLoteExiste()
    {
        var payload = new { strain = "Purple Basil" };
        var created = await _client.PostAsJsonAsync("/batches", payload);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetGuid();

        var response = await _client.GetAsync($"/batches/{id}/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
