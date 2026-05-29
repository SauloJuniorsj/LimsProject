using FluentValidation;
using LimsProject.Application.Models;

namespace LimsProject.Application.Validators;

public class SensorReadingValidator : AbstractValidator<SensorReading>
{
    public SensorReadingValidator()
    {
        RuleFor(x => x.Temperature)
            .InclusiveBetween(-10, 60)
            .WithMessage("Temperatura deve estar entre -10°C e 60°C.");
    }
}
