using LimsProject.Domain.Common;
using LimsProject.Domain.Enums;

namespace LimsProject.Domain.Entities;

public class Batch : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Strain { get; set; } = string.Empty;
    public BatchStatus Status { get; set; } = BatchStatus.Germination;
    public decimal? ThcPercentage { get; set; }
    public decimal? CbdPercentage { get; set; }
    public bool HasContaminants { get; set; }
    public decimal? CurrentMoisture { get; set; }
    public decimal? CurrentTemperature { get; set; }
    public decimal AverageTemperature { get; set; }

    // IAuditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // ISoftDeletable
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
