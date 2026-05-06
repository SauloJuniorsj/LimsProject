using LimsProject.Common.Results;

namespace LimsProject.Features.Sensors;

public interface ISensorIngestionService
{
    Task<Result<int>> IngestBulkAsync(BulkSensorRequest request, CancellationToken ct);
}
