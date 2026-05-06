using LimsProject.Common.Results;
using LimsProject.Features.Sensors;

namespace LimsProject.Features.Sensors.Alerts;

public interface IAlertThresholdService
{
    Task<Result<Guid>> CreateAsync(CreateThresholdRequest request, CancellationToken ct);
}
