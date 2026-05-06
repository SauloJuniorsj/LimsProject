using LimsProject.Common.Results;

namespace LimsProject.Features.Harvest;

public interface IHarvestService
{
    Task<Result<HarvestResponse>> RegisterAsync(Guid batchId, RegisterHarvestRequest request, CancellationToken ct);
}
