using LimsProject.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LimsProject.Infrastructure.HealthChecks;

public class RabbitMqHealthCheck(IRabbitMqClient client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await client.ProbeAsync(cancellationToken);
            return HealthCheckResult.Healthy("RabbitMQ channel open");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"RabbitMQ unreachable: {ex.Message}", ex);
        }
    }
}
