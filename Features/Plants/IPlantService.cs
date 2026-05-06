using LimsProject.Common.Results;

namespace LimsProject.Features.Plants;

public interface IPlantService
{
    Task<Result<PlantResponse>> RegisterAsync(Guid batchId, RegisterPlantRequest request, CancellationToken ct);
    Task<Result<PlantResponse>> UpdateAsync(Guid plantId, UpdatePlantRequest request, CancellationToken ct);
}
