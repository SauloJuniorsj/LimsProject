using LimsProject.Domain.Entities;
using LimsProject.Domain.Enums;

namespace LimsProject.Application.Models;

public record CertificateOfAnalysis(
    Guid BatchId,
    string Strain,
    BatchStatus Status,
    DateTime BatchCreatedAt,
    IEnumerable<LabAnalysis> Analyses,
    EnvironmentalConditions Environmental,
    IEnumerable<BatchStatusHistory> Lifecycle,
    ComplianceSummary Compliance,
    DateTime IssuedAt
);

public record EnvironmentalConditions(
    int DaysMonitored,
    decimal? OverallAvgTemperature,
    decimal? OverallMinTemperature,
    decimal? OverallMaxTemperature,
    int TotalReadings
);

public record ComplianceSummary(
    bool HasPassingAnalysis,
    bool HempCompliant,
    int AnalysisCount,
    DateTime? LastAnalysisDate
);
