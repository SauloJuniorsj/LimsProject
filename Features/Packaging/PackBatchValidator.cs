using FluentValidation;

namespace LimsProject.Features.Packaging;

public sealed class PackBatchValidator : AbstractValidator<PackBatchRequest>
{
    public PackBatchValidator()
    {
        RuleFor(x => x.FinishedProductId).NotEmpty();
        RuleFor(x => x.UnitCount).InclusiveBetween(1, 10_000);
        RuleFor(x => x.UnitWeightGrams).GreaterThan(0);
    }
}
