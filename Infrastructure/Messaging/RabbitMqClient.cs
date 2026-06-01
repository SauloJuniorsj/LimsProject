using LimsProject.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace LimsProject.Infrastructure.Messaging;

public interface IRabbitMqClient
{
    Task PublishAsync(string routingKey, ReadOnlyMemory<byte> payload, CancellationToken ct = default);
}

/// <summary>
/// Cliente AMQP para o broker. NÃO é um IEventPublisher — endpoints não falam com ele direto;
/// só o OutboxRelayWorker chama, despachando mensagens da tabela OutboxMessages.
///
/// Conexão e canal são lazy + thread-safe via SemaphoreSlim. Falhas de publicação são
/// PROPAGADAS (diferente do design fire-and-forget anterior) — o worker precisa saber pra
/// incrementar Attempts e fazer retry no próximo tick.
/// </summary>
public class RabbitMqClient(
    IConfiguration config,
    ILogger<RabbitMqClient> logger) : IRabbitMqClient, IAsyncDisposable
{
    public const string ExchangeName = "lims.events";

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(string routingKey, ReadOnlyMemory<byte> payload, CancellationToken ct = default)
    {
        var channel = await EnsureChannelAsync(ct);
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
            body: payload,
            cancellationToken: ct);

        logger.LogDebug("Published to {Exchange}/{Key}", ExchangeName, routingKey);
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
