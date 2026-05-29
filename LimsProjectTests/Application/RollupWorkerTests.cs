using FluentAssertions;
using LimsProject.Application.Services;
using Xunit;
using LimsProject.Application.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LimsProjectTests.Application;

public class RollupWorkerTests
{
    // Monta o cenário padrão: IRollupService mockado + IServiceScopeFactory configurado
    private static (RollupWorker worker, IRollupService rollupService) BuildWorker()
    {
        var rollupService = Substitute.For<IRollupService>();
        rollupService
            .ConsolidateDataAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider
            .GetService(typeof(IRollupService))
            .Returns(rollupService);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var logger = Substitute.For<ILogger<RollupWorker>>();
        var config = new ConfigurationBuilder().Build();

        return (new RollupWorker(scopeFactory, logger, config), rollupService);
    }

    [Fact]
    public async Task Worker_ExecutaConsolidacao_PeloMenosUmaVez()
    {
        var (worker, rollupService) = BuildWorker();
        using var cts = new CancellationTokenSource();

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(150); // dá tempo para a primeira iteração completar
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        await rollupService
            .Received()
            .ConsolidateDataAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Worker_CriaScopeParaCadaIteracao()
    {
        var rollupService = Substitute.For<IRollupService>();
        rollupService.ConsolidateDataAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IRollupService)).Returns(rollupService);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var logger = Substitute.For<ILogger<RollupWorker>>();
        var config = new ConfigurationBuilder().Build();
        var worker = new RollupWorker(scopeFactory, logger, config);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        // Verifica que o scope foi criado (padrão correto para serviços scoped em singleton worker)
        scopeFactory.Received().CreateScope();
    }
}
