namespace LimsProject.Features.Sensors;

public sealed class SensorData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public decimal Temperature { get; set; }
    public decimal? Humidity { get; set; }
    public decimal? Co2 { get; set; }
    public SensorReadingKind SensorType { get; set; } = SensorReadingKind.Temperature;
    public DateTime ReadingTime { get; set; }
}
