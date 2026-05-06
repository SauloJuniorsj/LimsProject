using LimsProject.Common.Persistence;
using LimsProject.Common.Results;
using LimsProject.Features.Traceability;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Features.Plants;

public sealed class PlantService(AppDbContext db, IChainOfCustodyWriter coc) : IPlantService
{
    public async Task<Result<PlantResponse>> RegisterAsync(Guid batchId, RegisterPlantRequest request, CancellationToken ct)
    {
        var batchExists = await db.Batches.AsNoTracking().AnyAsync(b => b.Id == batchId, ct);
        if (!batchExists)
            return Result<PlantResponse>.Failure("Lote não encontrado.");

        var tagTaken = await db.Plants.AsNoTracking().AnyAsync(p => p.TagCode == request.TagCode, ct);
        if (tagTaken)
            return Result<PlantResponse>.Failure("Tag já registrada.");

        var plant = new Plant
        {
            BatchId = batchId,
            TagCode = request.TagCode.Trim(),
            MotherPlantId = request.MotherPlantId,
            Status = PlantStatus.Alive,
        };
        db.Plants.Add(plant);
        coc.Append(batchId, CocEventTypes.PlantRegistered, new { plant.TagCode, plant.MotherPlantId }, null);
        await db.SaveChangesAsync(ct);
        return Result<PlantResponse>.Success(plant.ToResponse());
    }

    public async Task<Result<PlantResponse>> UpdateAsync(Guid plantId, UpdatePlantRequest request, CancellationToken ct)
    {
        var plant = await db.Plants.FirstOrDefaultAsync(p => p.Id == plantId, ct);
        if (plant is null)
            return Result<PlantResponse>.Failure("Planta não encontrada.");

        plant.Status = request.Status;
        coc.Append(plant.BatchId, CocEventTypes.PlantUpdated, new { plant.Id, request.Status }, null);
        await db.SaveChangesAsync(ct);
        return Result<PlantResponse>.Success(plant.ToResponse());
    }
}
