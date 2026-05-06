using LimsProject.Common.Persistence;
using LimsProject.Common.Results;
using LimsProject.Features.Plants;
using LimsProject.Features.Sensors;
using LimsProject.Features.Sensors.Rollup;
using LimsProject.Features.Traceability;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Features.Batches;

public sealed class BatchService(
    AppDbContext db,
    IBatchTransitionService transitions,
    IChainOfCustodyWriter coc) : IBatchService
{
    public async Task<Result<BatchResponse>> CreateAsync(CreateBatchRequest request, CancellationToken ct)
    {
        var seedLotExists = await db.SeedLots.AsNoTracking().AnyAsync(s => s.Id == request.SeedLotId, ct);
        if (!seedLotExists)
            return Result<BatchResponse>.Failure("Seed lot não encontrado.");

        var entity = request.ToEntity();
        db.Batches.Add(entity);
        coc.Append(entity.Id, CocEventTypes.BatchCreated, new { entity.SeedLotId, entity.RoomId }, null);
        await db.SaveChangesAsync(ct);
        return Result<BatchResponse>.Success(entity.ToResponse());
    }

    public async Task<BatchSummaryResponse?> GetSummaryAsync(Guid batchId, CancellationToken ct)
    {
        var batch = await db.Batches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null)
            return null;

        var since = DateTime.UtcNow.Date.AddDays(-7);

        var latestTemp = await db.SensorData.AsNoTracking()
            .Where(s => s.BatchId == batchId)
            .OrderByDescending(s => s.ReadingTime)
            .Select(s => (decimal?)s.Temperature)
            .FirstOrDefaultAsync(ct);

        var avg7 = await db.SensorData.AsNoTracking()
            .Where(s => s.BatchId == batchId && s.ReadingTime >= since)
            .Select(s => (decimal?)s.Temperature)
            .AverageAsync(ct);

        var activePlants = await db.Plants.AsNoTracking()
            .CountAsync(p => p.BatchId == batchId && p.Status == PlantStatus.Alive, ct);

        return new BatchSummaryResponse(batch.Id, batch.Status, latestTemp, avg7, activePlants);
    }

    public async Task<Result<BatchResponse>> TransitionAsync(Guid batchId, TransitionBatchRequest request, CancellationToken ct)
    {
        var batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null)
            return Result<BatchResponse>.Failure("Lote não encontrado.");

        var check = transitions.ValidateTransition(batch.Status, request.Target);
        if (!check.IsSuccess)
            return Result<BatchResponse>.Failure(check.Error!);

        batch.Status = request.Target;
        coc.Append(batch.Id, CocEventTypes.BatchTransition, new { request.Target, request.Reason }, null);
        await db.SaveChangesAsync(ct);
        return Result<BatchResponse>.Success(batch.ToResponse());
    }
}
