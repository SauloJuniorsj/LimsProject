namespace LimsProject.Features.Sensors.Alerts;

public sealed class AlertThreshold
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StrainId { get; set; }
    public decimal MinTemperature { get; set; }
    public decimal MaxTemperature { get; set; }
    public decimal? MaxHumidity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
