using LimsProject.Common.Results;

namespace LimsProject.Features.Batches;

public interface IBatchTransitionService
{
    Result ValidateTransition(BatchStatus from, BatchStatus to);
}
