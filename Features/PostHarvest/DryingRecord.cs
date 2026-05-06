namespace LimsProject.Features.PostHarvest;

public sealed class DryingRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HarvestId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public decimal? DryWeightGrams { get; set; }
}
