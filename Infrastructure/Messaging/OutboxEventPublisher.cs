using System.Text.Json;
using LimsProject.Application.Interfaces;
using LimsProject.Domain.Entities;

namespace LimsProject.Infrastructure.Messaging;

/// <summary>
/// Implementação outbox pattern de IEventPublisher: NÃO publica direto no broker.
///
/// Só adiciona um OutboxMessage ao DbContext (sem chamar SaveChanges) — o caller é
/// responsável por commitar. Isso garante que o estado da entidade e o evento são
/// salvos atomicamente na MESMA transação. O OutboxRelayWorker depois lê a tabela e
/// despacha pro RabbitMQ.
///
/// Resultado: zero dual-write, zero perda de evento por crash de processo entre
/// SaveChanges e Publish, eventos acumulam quando broker está fora.
/// </summary>
public class OutboxEventPublisher(ILimsDbContext db) : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : notnull
    {
        var typeName = typeof(T).Name;
        db.OutboxMessages.Add(new OutboxMessage
        {
            EventType = typeName,
            RoutingKey = $"lims.{typeName.ToLowerInvariant()}",
            Payload = JsonSerializer.Serialize(@event, JsonOptions)
        });
        return Task.CompletedTask;
    }
}
