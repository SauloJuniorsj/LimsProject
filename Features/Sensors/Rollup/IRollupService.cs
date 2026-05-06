namespace LimsProject.Features.Sensors.Rollup;

public interface IRollupService
{
    Task ConsolidateDataAsync(CancellationToken ct);
}
