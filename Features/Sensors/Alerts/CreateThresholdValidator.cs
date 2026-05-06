using FluentValidation;
using LimsProject.Features.Sensors;

namespace LimsProject.Features.Sensors.Alerts;

public sealed class CreateThresholdValidator : AbstractValidator<CreateThresholdRequest>
{
    public CreateThresholdValidator()
    {
        RuleFor(x => x.StrainId).NotEmpty();
        RuleFor(x => x.MinTemperature).LessThan(x => x.MaxTemperature).WithMessage("Min deve ser menor que Max.");
    }
}
