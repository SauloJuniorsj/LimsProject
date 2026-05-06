using LimsProject.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Features.Traceability;

public sealed class TraceabilityService(AppDbContext db) : ITraceabilityService
{
    public async Task<TraceResponse?> GetTraceBySerialAsync(string serialNumber, CancellationToken ct)
    {
        var pkg = await db.ProductPackages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.SerialNumber == serialNumber, ct);
        if (pkg is null)
            return null;

        var batch = await db.Batches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == pkg.BatchId, ct);
        if (batch is null)
            return null;

        var seedLot = await db.SeedLots.AsNoTracking().FirstOrDefaultAsync(s => s.Id == batch.SeedLotId, ct);
        if (seedLot is null)
            return null;

        var strain = await db.Strains.AsNoTracking().FirstOrDefaultAsync(s => s.Id == seedLot.StrainId, ct);
        if (strain is null)
            return null;

        var coa = await BuildCertificateAsync(pkg.BatchId, strain.Name, ct);

        var events = await db.ChainOfCustodyEvents.AsNoTracking()
            .Where(e => e.BatchId == batch.Id)
            .OrderBy(e => e.OccurredAt)
            .Select(e => new TraceStep(e.EventType, e.OccurredAt, e.PayloadJson))
            .ToListAsync(ct);

        return new TraceResponse(
            pkg.SerialNumber,
            batch.Id,
            seedLot.Id,
            seedLot.LotCode,
            strain.Id,
            strain.Name,
            coa,
            events);
    }

    public Task<CertificateOfAnalysisResponse?> GetCertificateAsync(Guid batchId, CancellationToken ct) =>
        BuildCertificateAsync(batchId, null, ct);

    private async Task<CertificateOfAnalysisResponse?> BuildCertificateAsync(Guid batchId, string? strainNameFallback, CancellationToken ct)
    {
        var strainName = strainNameFallback;
        if (strainName is null)
        {
            strainName = await (
                from b in db.Batches.AsNoTracking()
                join sl in db.SeedLots.AsNoTracking() on b.SeedLotId equals sl.Id
                join st in db.Strains.AsNoTracking() on sl.StrainId equals st.Id
                where b.Id == batchId
                select st.Name).FirstOrDefaultAsync(ct);
        }

        if (strainName is null)
            return null;

        var harvestDate = await db.HarvestRecords.AsNoTracking()
            .Where(h => h.BatchId == batchId)
            .OrderByDescending(h => h.HarvestDate)
            .Select(h => (DateTime?)h.HarvestDate)
            .FirstOrDefaultAsync(ct);

        var lab = await db.LabAnalyses.AsNoTracking()
            .Where(a => a.BatchId == batchId)
            .OrderByDescending(a => a.AnalysisDate)
            .FirstOrDefaultAsync(ct);

        return new CertificateOfAnalysisResponse(
            batchId,
            strainName,
            lab?.Thc,
            lab?.Cbd,
            lab?.Terpenes,
            harvestDate,
            lab?.AnalysisDate,
            lab?.IsPassed);
    }
}
