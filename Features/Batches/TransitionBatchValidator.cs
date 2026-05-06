using FluentValidation;

namespace LimsProject.Features.Batches;

public sealed class TransitionBatchValidator : AbstractValidator<TransitionBatchRequest>
{
    public TransitionBatchValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(512);
    }
}
