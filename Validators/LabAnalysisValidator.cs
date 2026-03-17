using FluentValidation;
using LimsProject.Models;

namespace LimsProject.Validators
{
    public class LabAnalysisValidator : AbstractValidator<LabAnalysis>
    {
        public LabAnalysisValidator()
        {
            RuleFor(x => x.THC).InclusiveBetween(0, 35).WithMessage("THC deve estar entre 0% e 35%");

            RuleFor(x => x.CBD).GreaterThanOrEqualTo(0).WithMessage("CBD deve pode ser negativo");

            // Regra de Negócio: Se for Cânhamo, o limite é rigoroso
            RuleFor(x => x.IsPassed)
                .Must((analysis, isPassed) =>
                {
                    if (analysis.THC > 0.3m && isPassed) return false;
                    return true;
                })
                .WithMessage("Um lote com mais de 0.3% de THC não pode ser marcado como 'Aprovado' para Cânhamo.");
        }
    }
}
