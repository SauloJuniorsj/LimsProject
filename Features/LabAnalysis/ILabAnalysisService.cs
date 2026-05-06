using LimsProject.Common.Results;

namespace LimsProject.Features.LabAnalysis;

public interface ILabAnalysisService
{
    Task<Result<LabAnalysisResponse>> SubmitAsync(Guid batchId, CreateLabAnalysisRequest request, CancellationToken ct);
}
