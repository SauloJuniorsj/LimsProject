using FluentValidation;

namespace LimsProject.Features.Sensors;

public sealed class BulkSensorValidator : AbstractValidator<BulkSensorRequest>
{
    public BulkSensorValidator()
    {
        RuleFor(x => x.Readings).NotEmpty().Must(x => x.Count <= 5000).WithMessage("Máximo 5000 leituras.");
        RuleForEach(x => x.Readings).ChildRules(reading =>
        {
            reading.RuleFor(r => r.BatchId).NotEmpty();
            reading.RuleFor(r => r.ReadingTime).NotEmpty();
        });
    }
}
