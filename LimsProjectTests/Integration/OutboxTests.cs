using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LimsProject.Application.Events;
using LimsProject.Application.Interfaces;
using LimsProject.Infrastructure.Workers;
using LimsProject.Domain.Entities;
using LimsProject.Infrastructure.Messaging;
using LimsProject.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace LimsProjectTests.Integration;

// ── Verifica o caminho HTTP: endpoint → OutboxMessage row ─────────────────────

public class OutboxRowCreationTests(OutboxFactory factory) : IClassFixture<OutboxFactory>
{
    [Fact]
    public async Task POST_Batches_GravaOutboxMessage_NaMesmaTransacao()
    {
        var strain = $"OutboxTest_{Guid.NewGuid():N}";
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        await client.PostAsJsonAsync("/batches", new { strain });

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // O IClassFixture compartilha DB — preciso achar EXATAMENTE o evento desse teste
        var allCreated = await db.OutboxMessages
            .Where(m => m.EventType == nameof(BatchCreatedEvent))
            .ToListAsync();
        var outbox = allCreated.FirstOrDefault(m => m.Payload.Contains(strain));

        outbox.Should().NotBeNull("o evento criado nesta chamada deve estar no outbox");
        outbox!.RoutingKey.Should().Be("lims.batchcreatedevent");
        outbox.PublishedAt.Should().BeNull();
        outbox.Attempts.Should().Be(0);

        using var doc = JsonDocument.Parse(outbox.Payload);
        doc.RootElement.GetProperty("strain").GetString().Should().Be(strain);
        doc.RootElement.GetProperty("batchId").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task PATCH_Status_GravaOutboxMessage_DoEventoStatusChanged()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        var resp = await client.PostAsJsonAsync("/batches", new { strain = "StatusOutbox" });
        var id = (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await client.PatchAsJsonAsync($"/batches/{id}/status", new { status = 1, reason = "test" });

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var statusEvents = await db.OutboxMessages
            .Where(m => m.EventType == nameof(BatchStatusChangedEvent))
            .ToListAsync();

        statusEvents.Should().NotBeEmpty();
        var payload = JsonDocument.Parse(statusEvents.Last().Payload).RootElement;
        payload.GetProperty("toStatus").GetInt32().Should().Be(1);
        payload.GetProperty("reason").GetString().Should().Be("test");
    }
}

// ── Verifica o worker: outbox pendente → IRabbitMqClient.PublishAsync ─────────

public class OutboxRelayWorkerTests
{
    private static AppDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(name).Options);

    private static OutboxRelayWorker BuildWorker(AppDbContext db, IRabbitMqClient client)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(AppDbContext)).Returns(db);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        return new OutboxRelayWorker(scopeFactory, client, NullLogger<OutboxRelayWorker>.Instance, config);
    }

    [Fact]
    public async Task Worker_PublicaPendentes_E_MarcaPublishedAt()
    {
        await using var db = CreateDb(nameof(Worker_PublicaPendentes_E_MarcaPublishedAt));
        db.OutboxMessages.Add(new OutboxMessage
        {
            EventType = "TestEvent",
            RoutingKey = "lims.testevent",
            Payload = """{"foo":"bar"}"""
        });
        await db.SaveChangesAsync();

        var client = Substitute.For<IRabbitMqClient>();
        var worker = BuildWorker(db, client);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(300); // dá tempo pra UM tick
        await worker.StopAsync(CancellationToken.None);

        await client.Received().PublishAsync("lims.testevent", Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());

        var msg = await db.OutboxMessages.SingleAsync();
        msg.PublishedAt.Should().NotBeNull();
        msg.Attempts.Should().Be(0);
    }

    [Fact]
    public async Task Worker_IncrementaAttempts_E_GravaLastError_QuandoBrokerFalha()
    {
        await using var db = CreateDb(nameof(Worker_IncrementaAttempts_E_GravaLastError_QuandoBrokerFalha));
        db.OutboxMessages.Add(new OutboxMessage
        {
            EventType = "TestEvent",
            RoutingKey = "lims.testevent",
            Payload = "{}"
        });
        await db.SaveChangesAsync();

        var client = Substitute.For<IRabbitMqClient>();
        client.PublishAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("broker offline simulado"));

        var worker = BuildWorker(db, client);
        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(300);
        await worker.StopAsync(CancellationToken.None);

        var msg = await db.OutboxMessages.SingleAsync();
        msg.PublishedAt.Should().BeNull();
        msg.Attempts.Should().BeGreaterThan(0);
        msg.LastError.Should().Contain("broker offline simulado");
    }
}

// ── Factory que habilita o publisher Outbox no Testing ────────────────────────

public class OutboxFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"OutboxTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(_dbName));

            // Remove o RollupWorker pra estabilidade
            var rollup = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IHostedService) &&
                d.ImplementationType?.Name == "RollupWorker");
            if (rollup is not null) services.Remove(rollup);

            // Sobrescreve IEventPublisher pra usar Outbox (não NullEventPublisher)
            var nullPub = services.SingleOrDefault(d => d.ServiceType == typeof(IEventPublisher));
            if (nullPub is not null) services.Remove(nullPub);
            services.AddScoped<IEventPublisher, OutboxEventPublisher>();
        });
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string role = "Admin")
    {
        var client = CreateClient();
        var email = $"outbox_{role.ToLower()}_{Guid.NewGuid():N}@test.com";
        const string password = "Test@1234";
        await client.PostAsJsonAsync("/auth/register", new { email, password, role });
        var loginResp = await client.PostAsJsonAsync("/auth/login", new { email, password });
        var token = (await loginResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
