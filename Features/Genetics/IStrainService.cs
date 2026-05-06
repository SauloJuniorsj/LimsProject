using LimsProject.Common.Results;

namespace LimsProject.Features.Genetics;

public interface IStrainService
{
    Task<Result<StrainResponse>> CreateAsync(CreateStrainRequest request, CancellationToken ct);
    Task<StrainResponse?> GetByIdAsync(Guid id, CancellationToken ct);
}
