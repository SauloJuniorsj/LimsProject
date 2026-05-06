using FluentValidation;

namespace LimsProject.Features.LabAnalysis;

public sealed class CreateLabAnalysisValidator : AbstractValidator<CreateLabAnalysisRequest>
{
    public CreateLabAnalysisValidator()
    {
        RuleFor(x => x.Thc).InclusiveBetween(0, 35);
        RuleFor(x => x.Cbd).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Terpenes).MaximumLength(1024);
        RuleFor(x => x.MoisturePercentage).InclusiveBetween(0, 100);
    }
}
