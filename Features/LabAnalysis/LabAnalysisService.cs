using LimsProject.Common.Persistence;
using LimsProject.Common.Results;
using LimsProject.Features.Batches;
using LimsProject.Features.Traceability;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Features.LabAnalysis;

public sealed class LabAnalysisService(
    AppDbContext db,
    IBatchTransitionService transitions,
    IChainOfCustodyWriter coc) : ILabAnalysisService
{
    public async Task<Result<LabAnalysisResponse>> SubmitAsync(Guid batchId, CreateLabAnalysisRequest request, CancellationToken ct)
    {
        var batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null)
            return Result<LabAnalysisResponse>.Failure("Lote não encontrado.");

        if (batch.Status != BatchStatus.Testing)
            return Result<LabAnalysisResponse>.Failure("Lote deve estar em Testing para submeter análise.");

        var strain = await (
            from b in db.Batches.AsNoTracking()
            join sl in db.SeedLots.AsNoTracking() on b.SeedLotId equals sl.Id
            join st in db.Strains.AsNoTracking() on sl.StrainId equals st.Id
            where b.Id == batchId
            select st).FirstOrDefaultAsync(ct);

        if (strain is null)
            return Result<LabAnalysisResponse>.Failure("Strain não encontrada para o lote.");

        if (request.HasContaminants && request.IsPassed)
            return Result<LabAnalysisResponse>.Failure("Contaminantes impedem aprovação.");

        if (request.IsPassed && request.Thc > strain.ThcMaxLimit)
            return Result<LabAnalysisResponse>.Failure($"THC acima do limite permitido ({strain.ThcMaxLimit}%).");

        var target = request.IsPassed ? BatchStatus.Released : BatchStatus.Rejected;
        var check = transitions.ValidateTransition(batch.Status, target);
        if (!check.IsSuccess)
            return Result<LabAnalysisResponse>.Failure(check.Error!);

        var entity = new LabAnalysis
        {
            BatchId = batchId,
            Thc = request.Thc,
            Cbd = request.Cbd,
            Terpenes = request.Terpenes.Trim(),
            MoisturePercentage = request.MoisturePercentage,
            HasContaminants = request.HasContaminants,
            AnalysisDate = DateTime.UtcNow,
            IsPassed = request.IsPassed,
        };

        db.LabAnalyses.Add(entity);
        batch.Status = target;
        coc.Append(batchId, CocEventTypes.LabAnalysisSubmitted, new { entity.Thc, entity.Cbd, entity.IsPassed }, null);
        await db.SaveChangesAsync(ct);

        return Result<LabAnalysisResponse>.Success(entity.ToResponse());
    }
}
