namespace LimsProject.Features.Packaging;

public sealed class FinishedProduct
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Guid StrainId { get; set; }
    public decimal UnitWeightGrams { get; set; }
    public ProductFormat Format { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
