namespace LimsProject.Domain.Entities;

public class BatchDailySummary
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public decimal AvgTemperature { get; set; }
    public DateTime Date { get; set; }
}
