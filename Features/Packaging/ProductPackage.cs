namespace LimsProject.Features.Packaging;

public sealed class ProductPackage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public Guid FinishedProductId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public decimal WeightGrams { get; set; }
    public DateTime PackagedAt { get; set; } = DateTime.UtcNow;
    public bool IsSold { get; set; }
}
