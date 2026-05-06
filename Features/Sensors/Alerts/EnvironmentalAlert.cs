namespace LimsProject.Features.Sensors.Alerts;

public sealed class EnvironmentalAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public Guid SensorDataId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool Resolved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
