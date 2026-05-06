namespace LimsProject.Features.Batches;

public sealed class Batch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SeedLotId { get; set; }
    public string? RoomId { get; set; }
    public BatchStatus Status { get; set; } = BatchStatus.Germination;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
