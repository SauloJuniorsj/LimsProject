namespace LimsProject.Features.Traceability;

public interface ITraceabilityService
{
    Task<TraceResponse?> GetTraceBySerialAsync(string serialNumber, CancellationToken ct);
    Task<CertificateOfAnalysisResponse?> GetCertificateAsync(Guid batchId, CancellationToken ct);
}
