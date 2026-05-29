using FluentAssertions;
using FluentValidation.TestHelper;
using LimsProject.Application.Models;
using LimsProject.Application.Validators;
using LimsProject.Domain.Entities;
using Xunit;

namespace LimsProjectTests.Unit;

// ── BatchValidator ─────────────────────────────────────────────────────────────

public class BatchValidatorTests
{
    private readonly BatchValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Strain_Vazio_EhInvalido(string strain)
    {
        var batch = new Batch { Strain = strain };
        _validator.TestValidate(batch).ShouldHaveValidationErrorFor(x => x.Strain);
    }

    [Fact]
    public void Strain_Preenchido_EhValido()
    {
        var batch = new Batch { Strain = "White Widow" };
        _validator.TestValidate(batch).ShouldNotHaveValidationErrorFor(x => x.Strain);
    }

    [Fact]
    public void Strain_AcimaDoMaximo_EhInvalido()
    {
        var batch = new Batch { Strain = new string('A', 101) };
        _validator.TestValidate(batch).ShouldHaveValidationErrorFor(x => x.Strain);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(35.1)]
    public void ThcPercentage_ForaDaFaixa_EhInvalido(double thc)
    {
        var batch = new Batch { Strain = "Mint", ThcPercentage = (decimal)thc };
        _validator.TestValidate(batch).ShouldHaveValidationErrorFor(x => x.ThcPercentage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.3)]
    [InlineData(35)]
    public void ThcPercentage_NaFaixa_EhValido(double thc)
    {
        var batch = new Batch { Strain = "Mint", ThcPercentage = (decimal)thc };
        _validator.TestValidate(batch).ShouldNotHaveValidationErrorFor(x => x.ThcPercentage);
    }

    [Fact]
    public void ThcPercentage_Nulo_EhValido()
    {
        var batch = new Batch { Strain = "Mint", ThcPercentage = null };
        _validator.TestValidate(batch).ShouldNotHaveValidationErrorFor(x => x.ThcPercentage);
    }

    [Theory]
    [InlineData(-11)]
    [InlineData(61)]
    public void CurrentTemperature_ForaDaFaixa_EhInvalida(double temp)
    {
        var batch = new Batch { Strain = "Mint", CurrentTemperature = (decimal)temp };
        _validator.TestValidate(batch).ShouldHaveValidationErrorFor(x => x.CurrentTemperature);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(-1)]
    public void CurrentMoisture_ForaDaFaixa_EhInvalida(double moisture)
    {
        var batch = new Batch { Strain = "Mint", CurrentMoisture = (decimal)moisture };
        _validator.TestValidate(batch).ShouldHaveValidationErrorFor(x => x.CurrentMoisture);
    }

    [Fact]
    public void Batch_TodosOsCamposValidos_EhValido()
    {
        var batch = new Batch
        {
            Strain = "Purple Basil",
            ThcPercentage = 0.2m,
            CbdPercentage = 8m,
            CurrentTemperature = 22m,
            CurrentMoisture = 65m
        };
        _validator.TestValidate(batch).IsValid.Should().BeTrue();
    }
}

// ── SensorReadingValidator ─────────────────────────────────────────────────────

public class SensorReadingValidatorTests
{
    private readonly SensorReadingValidator _validator = new();

    [Theory]
    [InlineData(-10.1)]
    [InlineData(60.1)]
    public void Temperatura_ForaDaFaixa_EhInvalida(double temp)
    {
        var reading = new SensorReading((decimal)temp);
        _validator.TestValidate(reading).ShouldHaveValidationErrorFor(x => x.Temperature);
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(60)]
    public void Temperatura_NaFaixa_EhValida(double temp)
    {
        var reading = new SensorReading((decimal)temp);
        _validator.TestValidate(reading).IsValid.Should().BeTrue();
    }

    [Fact]
    public void MensagemDeErro_TemTextoCorreto()
    {
        var result = _validator.TestValidate(new SensorReading(99m));
        result.ShouldHaveValidationErrorFor(x => x.Temperature)
            .WithErrorMessage("Temperatura deve estar entre -10°C e 60°C.");
    }
}
