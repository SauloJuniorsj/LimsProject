namespace LimsProject.Features.SeedLots;

public sealed class SeedLot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StrainId { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public string LotCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
