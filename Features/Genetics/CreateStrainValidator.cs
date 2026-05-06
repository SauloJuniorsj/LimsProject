using FluentValidation;

namespace LimsProject.Features.Genetics;

public sealed class CreateStrainValidator : AbstractValidator<CreateStrainRequest>
{
    public CreateStrainValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.ThcMaxLimit).InclusiveBetween(0, 100);
    }
}
