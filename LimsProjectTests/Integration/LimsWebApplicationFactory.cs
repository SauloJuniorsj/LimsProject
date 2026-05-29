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
}
