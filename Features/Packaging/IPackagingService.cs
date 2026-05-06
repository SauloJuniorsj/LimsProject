using LimsProject.Common.Results;

namespace LimsProject.Features.Packaging;

public interface IPackagingService
{
    Task<Result<FinishedProductResponse>> CreateFinishedProductAsync(CreateFinishedProductRequest request, CancellationToken ct);
    Task<Result<IReadOnlyList<ProductPackageResponse>>> PackBatchAsync(Guid batchId, PackBatchRequest request, CancellationToken ct);
    Task<Result> MarkPackageSoldAsync(string serialNumber, CancellationToken ct);
}
