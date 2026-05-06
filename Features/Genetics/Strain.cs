namespace LimsProject.Features.Genetics;

public sealed class Strain
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public StrainType Type { get; set; }
    /// <summary>Maximum allowed THC % for compliance (e.g. 0.3 for hemp).</summary>
    public decimal ThcMaxLimit { get; set; }
    public bool IsHemp { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
