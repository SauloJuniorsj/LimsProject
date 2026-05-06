using FluentValidation;

namespace LimsProject.Features.Packaging;

public sealed class CreateFinishedProductValidator : AbstractValidator<CreateFinishedProductRequest>
{
    public CreateFinishedProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.StrainId).NotEmpty();
        RuleFor(x => x.UnitWeightGrams).GreaterThan(0);
    }
}
