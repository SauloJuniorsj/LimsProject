namespace LimsProject.Features.LabAnalysis;

public sealed class LabAnalysis
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public decimal Thc { get; set; }
    public decimal Cbd { get; set; }
    public string Terpenes { get; set; } = string.Empty;
    public decimal MoisturePercentage { get; set; }
    public bool HasContaminants { get; set; }
    public DateTime AnalysisDate { get; set; }
    public bool IsPassed { get; set; }
}
