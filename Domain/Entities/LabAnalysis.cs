namespace LimsProject.Domain.Entities;

public class LabAnalysis
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public decimal THC { get; set; }
    public decimal CBD { get; set; }
    public string Terpenes { get; set; } = string.Empty;
    public DateTime AnalysisDate { get; set; }
    public bool IsPassed { get; set; }
}
