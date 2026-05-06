using FluentValidation;

namespace LimsProject.Features.SeedLots;

public sealed class CreateSeedLotValidator : AbstractValidator<CreateSeedLotRequest>
{
    public CreateSeedLotValidator()
    {
        RuleFor(x => x.StrainId).NotEmpty();
        RuleFor(x => x.Supplier).NotEmpty().MaximumLength(256);
        RuleFor(x => x.LotCode).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
