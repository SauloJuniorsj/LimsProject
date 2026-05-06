using LimsProject.Common.Persistence;
using LimsProject.Common.Results;
using LimsProject.Features.Sensors;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Features.Sensors.Alerts;

public sealed class AlertThresholdService(AppDbContext db) : IAlertThresholdService
{
    public async Task<Result<Guid>> CreateAsync(CreateThresholdRequest request, CancellationToken ct)
    {
        var exists = await db.Strains.AsNoTracking().AnyAsync(s => s.Id == request.StrainId, ct);
        if (!exists)
            return Result<Guid>.Failure("Strain não encontrado.");

        var th = new AlertThreshold
        {
            StrainId = request.StrainId,
            MinTemperature = request.MinTemperature,
            MaxTemperature = request.MaxTemperature,
            MaxHumidity = request.MaxHumidity,
        };
        db.AlertThresholds.Add(th);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Success(th.Id);
    }
}
