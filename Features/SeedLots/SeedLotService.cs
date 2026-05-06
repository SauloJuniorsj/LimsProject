using LimsProject.Common.Persistence;
using LimsProject.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Features.SeedLots;

public sealed class SeedLotService(AppDbContext db) : ISeedLotService
{
    public async Task<Result<SeedLotResponse>> CreateAsync(CreateSeedLotRequest request, CancellationToken ct)
    {
        var strainExists = await db.Strains.AsNoTracking().AnyAsync(s => s.Id == request.StrainId, ct);
        if (!strainExists)
            return Result<SeedLotResponse>.Failure("Strain não encontrado.");

        var entity = request.ToEntity();
        db.SeedLots.Add(entity);
        await db.SaveChangesAsync(ct);
        return Result<SeedLotResponse>.Success(entity.ToResponse());
    }
}
