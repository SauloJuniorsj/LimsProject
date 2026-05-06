using LimsProject.Common.Persistence;
using LimsProject.Common.Results;
using LimsProject.Features.Batches;
using LimsProject.Features.Traceability;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Features.Harvest;

public sealed class HarvestService(
    AppDbContext db,
    IBatchTransitionService transitions,
    IChainOfCustodyWriter coc) : IHarvestService
{
    public async Task<Result<HarvestResponse>> RegisterAsync(Guid batchId, RegisterHarvestRequest request, CancellationToken ct)
    {
        var batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null)
            return Result<HarvestResponse>.Failure("Lote não encontrado.");

        var check = transitions.ValidateTransition(batch.Status, BatchStatus.Harvested);
        if (!check.IsSuccess)
            return Result<HarvestResponse>.Failure(check.Error!);

        var harvest = new HarvestRecord
        {
            BatchId = batchId,
            WetWeightGrams = request.WetWeightGrams,
            OperatorId = request.OperatorId,
            HarvestDate = DateTime.UtcNow,
        };
        db.HarvestRecords.Add(harvest);
        batch.Status = BatchStatus.Harvested;
        coc.Append(batchId, CocEventTypes.HarvestRegistered, new { harvest.WetWeightGrams }, request.OperatorId);
        await db.SaveChangesAsync(ct);

        return Result<HarvestResponse>.Success(new HarvestResponse(
            harvest.Id,
            harvest.BatchId,
            harvest.HarvestDate,
            harvest.WetWeightGrams,
            harvest.OperatorId));
    }
}
