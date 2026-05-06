namespace LimsProject.Features.Sensors.Rollup;

/// <summary>Daily rollup row (legacy table name kept for migrations).</summary>
public sealed class BatchDailySummary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public decimal AvgTemperature { get; set; }
    public DateTime Date { get; set; }
}
