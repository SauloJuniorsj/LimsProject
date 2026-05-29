using LimsProject.Domain.Enums;

namespace LimsProject.Domain.Entities;

public class Batch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Strain { get; set; } = string.Empty;
    public BatchStatus Status { get; set; } = BatchStatus.Germination;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public decimal? ThcPercentage { get; set; }
    public decimal? CbdPercentage { get; set; }
    public bool HasContaminants { get; set; }
    public decimal? CurrentMoisture { get; set; }
    public decimal? CurrentTemperature { get; set; }
    public decimal AverageTemperature { get; set; }
}
