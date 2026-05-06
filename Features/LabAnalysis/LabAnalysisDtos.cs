namespace LimsProject.Features.LabAnalysis;

public sealed record CreateLabAnalysisRequest(
    decimal Thc,
    decimal Cbd,
    string Terpenes,
    decimal MoisturePercentage,
    bool HasContaminants,
    bool IsPassed);

public sealed record LabAnalysisResponse(
    Guid Id,
    Guid BatchId,
    decimal Thc,
    decimal Cbd,
    string Terpenes,
    decimal MoisturePercentage,
    bool HasContaminants,
    DateTime AnalysisDate,
    bool IsPassed);
