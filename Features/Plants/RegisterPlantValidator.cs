using FluentValidation;

namespace LimsProject.Features.Plants;

public sealed class RegisterPlantValidator : AbstractValidator<RegisterPlantRequest>
{
    public RegisterPlantValidator()
    {
        RuleFor(x => x.TagCode).NotEmpty().MaximumLength(64);
    }
}
