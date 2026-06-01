using LimsProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LimsProject.Infrastructure.HealthChecks;

/// <summary>
/// Mede o "lag" do outbox: quantas mensagens estão pendentes há mais de 1 minuto.
/// Se o worker está saudável, o lag deve ser ~zero. Lag crescente sinaliza broker
/// down, worker travado, ou throughput de eventos maior que o batchSize do worker.
///
/// Healthy: 0 mensagens atrasadas
/// Degraded: 1-9 atrasadas (atenção)
/// Unhealthy: 10+ atrasadas (broker provavelmente offline ou worker quebrado)
/// </summary>
public class OutboxLagHealthCheck(AppDbContext db) : IHealthCheck
{
    private static readonly TimeSpan LagThreshold = TimeSpan.FromMinutes(1);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - LagThreshold;
        var lagged = await db.OutboxMessages
            .CountAsync(m => m.PublishedAt == null && m.CreatedAt < cutoff, cancellationToken);

        var data = new Dictionary<string, object> { ["laggedMessageCount"] = lagged };

        return lagged switch
        {
            0 => HealthCheckResult.Healthy("No outbox lag", data),
            < 10 => HealthCheckResult.Degraded(
                $"{lagged} outbox message(s) pending > 1min", data: data),
            _ => HealthCheckResult.Unhealthy(
                $"{lagged} outbox messages stuck — broker likely down or worker stalled", data: data)
        };
    }
}
