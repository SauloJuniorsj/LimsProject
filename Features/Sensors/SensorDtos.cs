namespace LimsProject.Features.Sensors;

public sealed record SensorReadingDto(
    Guid BatchId,
    decimal Temperature,
    decimal? Humidity,
    DateTime ReadingTime);

public sealed record BulkSensorRequest(IReadOnlyList<SensorReadingDto> Readings);

public sealed record CreateThresholdRequest(
    Guid StrainId,
    decimal MinTemperature,
    decimal MaxTemperature,
    decimal? MaxHumidity);

public sealed record AlertResponse(
    Guid Id,
    Guid BatchId,
    Guid SensorDataId,
    string Message,
    bool Resolved,
    DateTime CreatedAt);
