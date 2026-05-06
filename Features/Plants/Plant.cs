namespace LimsProject.Features.Plants;

public sealed class Plant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public string TagCode { get; set; } = string.Empty;
    public PlantStatus Status { get; set; } = PlantStatus.Alive;
    public Guid? MotherPlantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
