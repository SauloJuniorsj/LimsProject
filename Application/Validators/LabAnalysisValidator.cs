using FluentValidation;
using LimsProject.Domain.Entities;

namespace LimsProject.Application.Validators;

public class LabAnalysisValidator : AbstractValidator<LabAnalysis>
{
    public LabAnalysisValidator()
    {
        RuleFor(x => x.THC)
            .InclusiveBetween(0, 35)
            .WithMessage("THC deve estar entre 0% e 35%");

        RuleFor(x => x.CBD)
            .GreaterThanOrEqualTo(0)
            .WithMessage("CBD não pode ser negativo");

        RuleFor(x => x.IsPassed)
            .Must((analysis, isPassed) => !(analysis.THC > 0.3m && isPassed))
            .WithMessage("Um lote com mais de 0.3% de THC não pode ser marcado como 'Aprovado' para Cânhamo.");
    }
}
