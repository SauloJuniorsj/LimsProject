using LimsProject.Common.Results;

namespace LimsProject.Features.SeedLots;

public interface ISeedLotService
{
    Task<Result<SeedLotResponse>> CreateAsync(CreateSeedLotRequest request, CancellationToken ct);
}
