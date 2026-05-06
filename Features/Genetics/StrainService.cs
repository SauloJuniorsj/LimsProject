using LimsProject.Common.Persistence;
using LimsProject.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Features.Genetics;

public sealed class StrainService(AppDbContext db) : IStrainService
{
    public async Task<Result<StrainResponse>> CreateAsync(CreateStrainRequest request, CancellationToken ct)
    {
        var entity = request.ToEntity();
        db.Strains.Add(entity);
        await db.SaveChangesAsync(ct);
        return Result<StrainResponse>.Success(entity.ToResponse());
    }

    public async Task<StrainResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var e = await db.Strains.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        return e?.ToResponse();
    }
}
