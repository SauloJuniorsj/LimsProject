using System.Text;
using LimsProject.Infrastructure.Messaging;
using LimsProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LimsProject.Infrastructure.Workers;

/// <summary>
/// Despacha mensagens da tabela OutboxMessages pro RabbitMQ.
///
/// Polla a cada `Outbox:PollIntervalSeconds` (default 2s). Pra cada mensagem PublishedAt=null
/// com Attempts &lt; MaxAttempts: publica, marca PublishedAt em sucesso, ou incrementa Attempts
/// e grava LastError em falha. Quando MaxAttempts é atingido, a mensagem fica órfã (dead letter
/// implícito — pronta pra ser inspecionada via query SQL).
///
/// Ordering: FIFO por CreatedAt. Pega `Outbox:BatchSize` mensagens por tick.
/// </summary>
public class OutboxRelayWorker(
    IServiceScopeFactory scopeFactory,
    IRabbitMqClient rabbitClient,
    ILogger<OutboxRelayWorker> logger,
    IConfiguration config) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(
        config.GetValue("Outbox:PollIntervalSeconds", 2));
    private readonly int _batchSize = config.GetValue("Outbox:BatchSize", 50);
    private readonly int _maxAttempts = config.GetValue("Outbox:MaxAttempts", 5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "OutboxRelayWorker iniciado: interval={Interval}s, batchSize={Batch}, maxAttempts={Max}",
            _interval.TotalSeconds, _batchSize, _maxAttempts);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OutboxRelayWorker tick falhou inesperadamente — vai tentar de novo");
            }

            try { await Task.Delay(_interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task DispatchBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pending = await db.OutboxMessages
            .Where(m => m.PublishedAt == null && m.Attempts < _maxAttempts)
            .OrderBy(m => m.CreatedAt)
            .Take(_batchSize)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        var now = DateTime.UtcNow;
        var published = 0;
        var failed = 0;

        foreach (var msg in pending)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(msg.Payload);
                await rabbitClient.PublishAsync(msg.RoutingKey, body, ct);
                msg.PublishedAt = now;
                published++;
            }
            catch (Exception ex)
            {
                msg.Attempts++;
                msg.LastError = ex.Message;
                failed++;
                logger.LogWarning(ex,
                    "Falha publicando outbox message {Id} (attempt {Attempt}/{Max})",
                    msg.Id, msg.Attempts, _maxAttempts);
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Outbox tick: {Published} publicados, {Failed} falharam", published, failed);
    }
}
