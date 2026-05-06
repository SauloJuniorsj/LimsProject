using LimsProject.Common.Results;

namespace LimsProject.Features.Batches;

public interface IBatchService
{
    Task<Result<BatchResponse>> CreateAsync(CreateBatchRequest request, CancellationToken ct);
    Task<BatchSummaryResponse?> GetSummaryAsync(Guid batchId, CancellationToken ct);
    Task<Result<BatchResponse>> TransitionAsync(Guid batchId, TransitionBatchRequest request, CancellationToken ct);
}
