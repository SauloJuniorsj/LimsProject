using LimsProject.Common.Results;

namespace LimsProject.Features.PostHarvest;

public interface IPostHarvestService
{
    Task<Result<Guid>> StartDryingAsync(Guid harvestId, CancellationToken ct);
    Task<Result> CompleteDryingAsync(Guid dryingId, CompleteDryingRequest request, CancellationToken ct);
    Task<Result<Guid>> StartCuringAsync(Guid dryingId, CancellationToken ct);
    Task<Result> CompleteCuringAsync(Guid curingId, CompleteCuringRequest request, CancellationToken ct);
}
