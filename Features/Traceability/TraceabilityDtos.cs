namespace LimsProject.Features.Traceability;

public sealed record CertificateOfAnalysisResponse(
    Guid BatchId,
    string StrainName,
    decimal? Thc,
    decimal? Cbd,
    string? Terpenes,
    DateTime? HarvestDate,
    DateTime? LabDate,
    bool? Passed);

public sealed record TraceStep(string EventType, DateTime OccurredAt, string PayloadJson);

public sealed record TraceResponse(
    string SerialNumber,
    Guid BatchId,
    Guid SeedLotId,
    string LotCode,
    Guid StrainId,
    string StrainName,
    CertificateOfAnalysisResponse? Certificate,
    IReadOnlyList<TraceStep> ChainOfCustody);
