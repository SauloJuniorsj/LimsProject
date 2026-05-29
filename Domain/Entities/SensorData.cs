namespace LimsProject.Domain.Entities;

public class SensorData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public decimal Temperature { get; set; }
    public DateTime ReadingTime { get; set; }
}
