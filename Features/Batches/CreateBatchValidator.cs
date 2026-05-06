using FluentValidation;

namespace LimsProject.Features.Batches;

public sealed class CreateBatchValidator : AbstractValidator<CreateBatchRequest>
{
    public CreateBatchValidator()
    {
        RuleFor(x => x.SeedLotId).NotEmpty();
        RuleFor(x => x.RoomId).MaximumLength(128);
    }
}
