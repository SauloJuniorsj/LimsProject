namespace LimsProject.Domain.Entities;

public class BatchDailySummary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public decimal AvgTemperature { get; set; }
    public decimal MinTemperature { get; set; }
    public decimal MaxTemperature { get; set; }
    public int ReadingCount { get; set; }
    public DateTime Date { get; set; }
}
