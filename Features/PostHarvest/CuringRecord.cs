namespace LimsProject.Features.PostHarvest;

public sealed class CuringRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DryingId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public decimal? FinalMoisture { get; set; }
}
