using FluentValidation;

namespace LimsProject.Features.Harvest;

public sealed class RegisterHarvestValidator : AbstractValidator<RegisterHarvestRequest>
{
    public RegisterHarvestValidator()
    {
        RuleFor(x => x.WetWeightGrams).GreaterThan(0);
    }
}
