using FluentValidation;
using LimsProject.Domain.Entities;

namespace LimsProject.Application.Validators;

public class BatchValidator : AbstractValidator<Batch>
{
    public BatchValidator()
    {
        RuleFor(x => x.Strain)
            .NotEmpty().WithMessage("Strain é obrigatório.")
            .MaximumLength(100).WithMessage("Strain deve ter no máximo 100 caracteres.");

        RuleFor(x => x.ThcPercentage)
            .InclusiveBetween(0, 35).When(x => x.ThcPercentage.HasValue)
            .WithMessage("THC deve estar entre 0% e 35%.");

        RuleFor(x => x.CbdPercentage)
            .GreaterThanOrEqualTo(0).When(x => x.CbdPercentage.HasValue)
            .WithMessage("CBD não pode ser negativo.");

        RuleFor(x => x.CurrentTemperature)
            .InclusiveBetween(-10, 60).When(x => x.CurrentTemperature.HasValue)
            .WithMessage("Temperatura deve estar entre -10°C e 60°C.");

        RuleFor(x => x.CurrentMoisture)
            .InclusiveBetween(0, 100).When(x => x.CurrentMoisture.HasValue)
            .WithMessage("Umidade deve estar entre 0% e 100%.");
    }
}
