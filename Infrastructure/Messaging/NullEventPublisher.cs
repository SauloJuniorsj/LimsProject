using LimsProject.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace LimsProject.Infrastructure.Messaging;

/// <summary>
/// No-op publisher usado quando o broker está desabilitado (testes ou ambiente sem fila).
/// Loga em Debug para facilitar troubleshooting sem custo de produção.
/// </summary>
public class NullEventPublisher(ILogger<NullEventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : notnull
    {
        logger.LogDebug("[NullPublisher] {Type}: {@Event}", typeof(T).Name, @event);
        return Task.CompletedTask;
    }
}
