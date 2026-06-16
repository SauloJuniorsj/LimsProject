using System.Reflection;
using FluentAssertions;
using LimsProject.Domain.Entities;
using NetArchTest.Rules;
using Xunit;

namespace LimsProjectTests.Architecture;

/// <summary>
/// Architecture fitness functions — Onion layer dependency rules.
/// These tests fail the build when someone violates the inward-only dependency direction.
/// </summary>
public class OnionArchitectureTests
{
    private static readonly Assembly ProductionAssembly = typeof(Batch).Assembly;

    private const string Domain         = "LimsProject.Domain";
    private const string Application    = "LimsProject.Application";
    private const string Infrastructure = "LimsProject.Infrastructure";
    private const string Api            = "LimsProject.API";

    [Fact]
    public void Domain_NaoDependeDeNenhumaOutraCamada()
    {
        var result = Types.InAssembly(ProductionAssembly)
            .That().ResideInNamespace(Domain)
            .Should().NotHaveDependencyOnAny(Application, Infrastructure, Api)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain é o núcleo — não pode olhar pra fora. Violadores: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_NaoDependeDeInfrastructureNemApi()
    {
        var result = Types.InAssembly(ProductionAssembly)
            .That().ResideInNamespace(Application)
            .Should().NotHaveDependencyOnAny(Infrastructure, Api)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application pode depender só de Domain. Violadores: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Infrastructure_NaoDependeDeApi()
    {
        var result = Types.InAssembly(ProductionAssembly)
            .That().ResideInNamespace(Infrastructure)
            .Should().NotHaveDependencyOn(Api)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Infrastructure não conhece a camada de entrega (API). Violadores: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Entities_NaoDependemDeEntityFrameworkCore()
    {
        var result = Types.InAssembly(ProductionAssembly)
            .That().ResideInNamespace("LimsProject.Domain.Entities")
            .Should().NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Entidades de domínio devem ser POCOs — sem ORM. Violadores: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Endpoints_NaoUsamDbContextConcretoDoEfCore()
    {
        var result = Types.InAssembly(ProductionAssembly)
            .That().ResideInNamespace("LimsProject.API.Endpoints")
            .Should().NotHaveDependencyOn("LimsProject.Infrastructure.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Endpoints devem injetar ILimsDbContext (abstração), não o LimsDbContext concreto. Violadores: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }
}
