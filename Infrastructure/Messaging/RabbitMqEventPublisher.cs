using System.Text.Json;
using LimsProject.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace LimsProject.Infrastructure.Messaging;

/// <summary>
/// Publica eventos de domínio num exchange topic do RabbitMQ.
///
/// Conexão e canal são inicializados lazy na primeira publicação (com lock pra ser thread-safe).
/// Falhas de publicação são logadas mas não propagadas — broker indisponível NÃO derruba o request.
/// Em produção real, isso deveria ir pra um outbox pattern, mas pra portfolio é fire-and-forget.
/// </summary>
public class RabbitMqEventPublisher(
    IConfiguration config,
    ILogger<RabbitMqEventPublisher> logger) : IEventPublisher, IAsyncDisposable
{
    private const string ExchangeName = "lims.events";
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : notnull
    {
        try
        {
            var channel = await EnsureChannelAsync(ct);
            var routingKey = $"lims.{typeof(T).Name.ToLowerInvariant()}";
            var body = JsonSerializer.SerializeToUtf8Bytes(@event);
            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await channel.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: ct);

            logger.LogDebug("Published {Type} with routing key {Key}", typeof(T).Name, routingKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao publicar evento {Type} no RabbitMQ", typeof(T).Name);
        }
    }

    private async Task<IChannel> EnsureChannelAsync(CancellationToken ct)
    {
        if (_channel?.IsOpen == true) return _channel;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_channel?.IsOpen == true) return _channel;

            var factory = new ConnectionFactory
            {
                HostName = config["RabbitMq:Host"] ?? "broker",
                Port = config.GetValue("RabbitMq:Port", 5672),
                UserName = config["RabbitMq:User"] ?? "guest",
                Password = config["RabbitMq:Password"] ?? "guest"
            };

            _connection = await factory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
            await _channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: ct);

            logger.LogInformation("RabbitMQ conectado em {Host}:{Port}, exchange {Exchange}",
                factory.HostName, factory.Port, ExchangeName);

            return _channel;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
        _initLock.Dispose();
    }
}
