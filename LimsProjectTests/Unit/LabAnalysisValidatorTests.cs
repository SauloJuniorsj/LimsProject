using FluentAssertions;
using LimsProject.Application.Validators;
using LimsProject.Domain.Entities;
using Xunit;

namespace LimsProjectTests.Unit;

public class LabAnalysisValidatorTests
{
    private readonly LabAnalysisValidator _validator = new();

    // ── THC ────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.3)]
    [InlineData(17.5)]
    [InlineData(35.0)]
    public async Task THC_DentroDoIntervalo_Valido(double thc)
    {
        var analysis = Build(thc: (decimal)thc, cbd: 0, isPassed: false);
        var result = await _validator.ValidateAsync(analysis);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(35.01)]
    public async Task THC_ForaDoIntervalo_Invalido(double thc)
    {
        var analysis = Build(thc: (decimal)thc, cbd: 0, isPassed: false);
        var result = await _validator.ValidateAsync(analysis);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "THC");
    }

    [Fact]
    public async Task THC_ForaDoIntervalo_MensagemDeErroCorreta()
    {
        var analysis = Build(thc: 36m, cbd: 0, isPassed: false);
        var result = await _validator.ValidateAsync(analysis);
        result.Errors.Should().Contain(e => e.PropertyName == "THC" && e.ErrorMessage.Contains("35%"));
    }

    // ── CBD ────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0)]
    [InlineData(5.5)]
    public async Task CBD_MaiorOuIgualZero_Valido(double cbd)
    {
        var analysis = Build(thc: 1m, cbd: (decimal)cbd, isPassed: false);
        var result = await _validator.ValidateAsync(analysis);
        result.Errors.Should().NotContain(e => e.PropertyName == "CBD");
    }

    [Fact]
    public async Task CBD_Negativo_Invalido()
    {
        var analysis = Build(thc: 1m, cbd: -0.01m, isPassed: false);
        var result = await _validator.ValidateAsync(analysis);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CBD");
    }

    // ── Regra de compliance de cânhamo ─────────────────────────────────────────

    [Fact]
    public async Task HempCompliance_THCAbove03_ComIsPassed_Invalido()
    {
        var analysis = Build(thc: 0.31m, cbd: 5m, isPassed: true);
        var result = await _validator.ValidateAsync(analysis);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IsPassed");
    }

    [Fact]
    public async Task HempCompliance_THCAbove03_SemIsPassed_Valido()
    {
        var analysis = Build(thc: 0.31m, cbd: 5m, isPassed: false);
        var result = await _validator.ValidateAsync(analysis);
        result.Errors.Should().NotContain(e => e.PropertyName == "IsPassed");
    }

    [Fact]
    public async Task HempCompliance_THCAbaixo03_ComIsPassed_Valido()
    {
        var analysis = Build(thc: 0.29m, cbd: 5m, isPassed: true);
        var result = await _validator.ValidateAsync(analysis);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task HempCompliance_THCExatamente03_ComIsPassed_Valido()
    {
        var analysis = Build(thc: 0.3m, cbd: 5m, isPassed: true);
        var result = await _validator.ValidateAsync(analysis);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task HempCompliance_MensagemDeErroCorreta()
    {
        var analysis = Build(thc: 1m, cbd: 0, isPassed: true);
        var result = await _validator.ValidateAsync(analysis);
        result.Errors.Should().Contain(e =>
            e.PropertyName == "IsPassed" &&
            e.ErrorMessage.Contains("0.3%"));
    }

    // ── Análise completamente válida ───────────────────────────────────────────

    [Fact]
    public async Task AnaliseCompleta_Valida_RetornaIsValidTrue()
    {
        var analysis = Build(thc: 0.2m, cbd: 8.5m, isPassed: true);
        var result = await _validator.ValidateAsync(analysis);
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    // ── Helper ─────────────────────────────────────────────────────────────────

    private static LabAnalysis Build(decimal thc, decimal cbd, bool isPassed) =>
        new() { THC = thc, CBD = cbd, IsPassed = isPassed, Terpenes = "citrus" };
}
