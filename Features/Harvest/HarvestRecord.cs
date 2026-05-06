namespace LimsProject.Features.Harvest;

public sealed class HarvestRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public DateTime HarvestDate { get; set; } = DateTime.UtcNow;
    public decimal WetWeightGrams { get; set; }
    public Guid? OperatorId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
