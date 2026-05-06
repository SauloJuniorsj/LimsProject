using LimsProject.Common.Persistence;
using LimsProject.Common.Results;
using LimsProject.Features.Batches;
using LimsProject.Features.Traceability;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Features.PostHarvest;

public sealed class PostHarvestService(
    AppDbContext db,
    IBatchTransitionService transitions,
    IChainOfCustodyWriter coc) : IPostHarvestService
{
    public async Task<Result<Guid>> StartDryingAsync(Guid harvestId, CancellationToken ct)
    {
        var harvest = await db.HarvestRecords.AsNoTracking().FirstOrDefaultAsync(x => x.Id == harvestId, ct);
        if (harvest is null)
            return Result<Guid>.Failure("Colheita não encontrada.");

        var batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == harvest.BatchId, ct);
        if (batch is null)
            return Result<Guid>.Failure("Lote não encontrado.");

        var check = transitions.ValidateTransition(batch.Status, BatchStatus.Drying);
        if (!check.IsSuccess)
            return Result<Guid>.Failure(check.Error!);

        var drying = new DryingRecord { HarvestId = harvestId, StartedAt = DateTime.UtcNow };
        db.DryingRecords.Add(drying);
        batch.Status = BatchStatus.Drying;
        coc.Append(batch.Id, CocEventTypes.DryingStarted, new { harvestId }, null);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Success(drying.Id);
    }

    public async Task<Result> CompleteDryingAsync(Guid dryingId, CompleteDryingRequest request, CancellationToken ct)
    {
        var drying = await db.DryingRecords.FirstOrDefaultAsync(d => d.Id == dryingId, ct);
        if (drying is null)
            return Result.Failure("Secagem não encontrada.");

        var harvest = await db.HarvestRecords.FirstOrDefaultAsync(h => h.Id == drying.HarvestId, ct);
        if (harvest is null)
            return Result.Failure("Colheita não encontrada.");

        if (request.DryWeightGrams > harvest.WetWeightGrams)
            return Result.Failure("Peso seco não pode exceder peso úmido.");

        drying.DryWeightGrams = request.DryWeightGrams;
        drying.EndedAt = DateTime.UtcNow;
        coc.Append(harvest.BatchId, CocEventTypes.DryingCompleted, new { dryingId, request.DryWeightGrams }, null);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<Guid>> StartCuringAsync(Guid dryingId, CancellationToken ct)
    {
        var drying = await db.DryingRecords.FirstOrDefaultAsync(d => d.Id == dryingId, ct);
        if (drying is null)
            return Result<Guid>.Failure("Secagem não encontrada.");

        if (drying.DryWeightGrams is null || drying.EndedAt is null)
            return Result<Guid>.Failure("Finalize a secagem antes de iniciar cura.");

        var harvest = await db.HarvestRecords.FirstOrDefaultAsync(h => h.Id == drying.HarvestId, ct);
        if (harvest is null)
            return Result<Guid>.Failure("Colheita não encontrada.");

        var batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == harvest.BatchId, ct);
        if (batch is null)
            return Result<Guid>.Failure("Lote não encontrado.");

        var check = transitions.ValidateTransition(batch.Status, BatchStatus.Curing);
        if (!check.IsSuccess)
            return Result<Guid>.Failure(check.Error!);

        var curing = new CuringRecord { DryingId = dryingId, StartedAt = DateTime.UtcNow };
        db.CuringRecords.Add(curing);
        batch.Status = BatchStatus.Curing;
        coc.Append(batch.Id, CocEventTypes.CuringStarted, new { dryingId }, null);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Success(curing.Id);
    }

    public async Task<Result> CompleteCuringAsync(Guid curingId, CompleteCuringRequest request, CancellationToken ct)
    {
        var curing = await db.CuringRecords.FirstOrDefaultAsync(c => c.Id == curingId, ct);
        if (curing is null)
            return Result.Failure("Cura não encontrada.");

        var drying = await db.DryingRecords.FirstOrDefaultAsync(d => d.Id == curing.DryingId, ct);
        if (drying is null)
            return Result.Failure("Secagem não encontrada.");

        var harvest = await db.HarvestRecords.FirstOrDefaultAsync(h => h.Id == drying.HarvestId, ct);
        if (harvest is null)
            return Result.Failure("Colheita não encontrada.");

        var batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == harvest.BatchId, ct);
        if (batch is null)
            return Result.Failure("Lote não encontrado.");

        var check = transitions.ValidateTransition(batch.Status, BatchStatus.Testing);
        if (!check.IsSuccess)
            return Result.Failure(check.Error!);

        curing.FinalMoisture = request.FinalMoisture;
        curing.EndedAt = DateTime.UtcNow;
        batch.Status = BatchStatus.Testing;
        coc.Append(batch.Id, CocEventTypes.CuringCompleted, new { curingId, request.FinalMoisture }, null);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
