namespace LimsProject.Features.Sensors.Alerts;

public interface IAlertService
{
    Task EvaluateBulkAsync(IReadOnlyList<SensorData> rows, CancellationToken ct);
}
