using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LimsProject.Application.Workers;
using LimsProject.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LimsProjectTests.Integration;

public class LimsWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"LimsTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Program.cs skips AddDbContext in Testing env — add InMemory here instead
            services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(_dbName));

            // Remove RollupWorker hosted service to prevent TestServer crashes
            var worker = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IHostedService) &&
                d.ImplementationType == typeof(RollupWorker));
            if (worker is not null) services.Remove(worker);
        });
    }

    public async Task<string> GetTokenAsync(HttpClient client, string role = "Admin")
    {
        var email = $"{role.ToLower()}_{Guid.NewGuid():N}@test.com";
        const string password = "Test@1234";

        await client.PostAsJsonAsync("/auth/register", new { email, password, role });

        var response = await client.PostAsJsonAsync("/auth/login", new { email, password });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string role = "Admin")
    {
        var client = CreateClient();
        var token = await GetTokenAsync(client, role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
