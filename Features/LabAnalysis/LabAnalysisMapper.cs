namespace LimsProject.Features.LabAnalysis;

public static class LabAnalysisMapper
{
    public static LabAnalysisResponse ToResponse(this LabAnalysis a) =>
        new(a.Id, a.BatchId, a.Thc, a.Cbd, a.Terpenes, a.MoisturePercentage, a.HasContaminants, a.AnalysisDate, a.IsPassed);
}
