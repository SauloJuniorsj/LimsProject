using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace LimsProjectTests.Integration;

public class UsersEndpointsTests(LimsWebApplicationFactory factory)
    : IClassFixture<LimsWebApplicationFactory>
{
    private async Task<string> CreateUserAndGetIdAsync(string role)
    {
        var anon = factory.CreateClient();
        var email = $"target_{role.ToLower()}_{Guid.NewGuid():N}@test.com";
        await anon.PostAsJsonAsync("/auth/register", new { email, password = "Test@1234", role });

        // Listamos os usuários (precisa de admin) pra achar o id que acabamos de criar
        var admin = await factory.CreateAuthenticatedClientAsync("Admin");
        var resp = await admin.GetAsync($"/users?email={Uri.EscapeDataString(email)}");
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("items")[0].GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task GET_Users_Retorna200_ComListaPaginada()
    {
        var admin = await factory.CreateAuthenticatedClientAsync("Admin");

        var resp = await admin.GetAsync("/users?pageSize=10");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(0);
        body.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
        var first = body.GetProperty("items")[0];
        first.GetProperty("email").GetString().Should().NotBeNullOrEmpty();
        first.GetProperty("roles").GetArrayLength().Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GET_Users_Retorna403_ComRoleLab()
    {
        var lab = await factory.CreateAuthenticatedClientAsync("Lab");
        var resp = await lab.GetAsync("/users");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GET_Users_Retorna401_SemToken()
    {
        var anon = factory.CreateClient();
        var resp = await anon.GetAsync("/users");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_Users_FiltrarPorEmail_RetornaApenasMatchs()
    {
        var admin = await factory.CreateAuthenticatedClientAsync("Admin");
        var email = $"uniquefilter_{Guid.NewGuid():N}@test.com";
        await admin.PostAsJsonAsync("/auth/register", new { email, password = "Test@1234", role = "Lab" });

        var resp = await admin.GetAsync($"/users?email={Uri.EscapeDataString(email)}");
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("totalCount").GetInt32().Should().Be(1);
        body.GetProperty("items")[0].GetProperty("email").GetString().Should().Be(email);
    }

    [Fact]
    public async Task DELETE_User_Retorna204()
    {
        var targetId = await CreateUserAndGetIdAsync("Lab");
        var admin = await factory.CreateAuthenticatedClientAsync("Admin");

        var resp = await admin.DeleteAsync($"/users/{targetId}");

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_User_Retorna422_QuandoTentaApagarPropriaConta()
    {
        // Esta é a parte mais importante de segurança
        var admin = factory.CreateClient();
        var email = $"selfdel_{Guid.NewGuid():N}@test.com";
        await admin.PostAsJsonAsync("/auth/register", new { email, password = "Test@1234", role = "Admin" });

        var login = await admin.PostAsJsonAsync("/auth/login", new { email, password = "Test@1234" });
        var tokens = await login.Content.ReadFromJsonAsync<JsonElement>();
        admin.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", tokens.GetProperty("accessToken").GetString());

        // Pega o próprio id
        var meResp = await admin.GetAsync($"/users?email={Uri.EscapeDataString(email)}");
        var meBody = await meResp.Content.ReadFromJsonAsync<JsonElement>();
        var myId = meBody.GetProperty("items")[0].GetProperty("id").GetString()!;

        var delResp = await admin.DeleteAsync($"/users/{myId}");

        delResp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DELETE_User_Retorna404_QuandoNaoExiste()
    {
        var admin = await factory.CreateAuthenticatedClientAsync("Admin");
        var resp = await admin.DeleteAsync($"/users/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_Role_AlteraRoleDoUsuario()
    {
        var targetId = await CreateUserAndGetIdAsync("Lab");
        var admin = await factory.CreateAuthenticatedClientAsync("Admin");

        var resp = await admin.PutAsJsonAsync($"/users/{targetId}/role", new { role = "Admin" });

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verifica
        var listResp = await admin.GetAsync($"/users?pageSize=200");
        var body = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        var target = body.GetProperty("items").EnumerateArray()
            .First(i => i.GetProperty("id").GetString() == targetId);
        target.GetProperty("roles").EnumerateArray()
            .Select(r => r.GetString()).Should().Contain("Admin");
    }

    [Fact]
    public async Task PUT_Role_Retorna400_ComRoleInvalida()
    {
        var targetId = await CreateUserAndGetIdAsync("Lab");
        var admin = await factory.CreateAuthenticatedClientAsync("Admin");

        var resp = await admin.PutAsJsonAsync($"/users/{targetId}/role", new { role = "Hacker" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
