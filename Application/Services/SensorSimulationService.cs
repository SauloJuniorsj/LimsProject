using LimsProject.Application.Events;
using LimsProject.Application.Interfaces;
using LimsProject.Application.Models;
using LimsProject.Domain.Entities;
using LimsProject.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LimsProject.Application.Services;

public interface ISensorSimulationService
{
    /// <summary>Duração total (segundos) de um burst — usado pelo front pra cronometrar a UI "ao vivo".</summary>
    int TotalDurationSeconds { get; }

    /// <summary>Dispara um burst de leituras simuladas em background. False se já há uma rodando pro lote.</summary>
    bool TryStart(Guid batchId);
}

/// <summary>
/// Gera leituras de sensor sintéticas (random walk, com spikes propositais fora da faixa
/// 18-30°C) pra um lote, uma a cada IntervalSeconds. Roda fora do request scope via
/// IServiceScopeFactory — mesmo padrão do RollupWorker — porque o burst dura mais que
/// o request HTTP que o disparou.
/// </summary>
public class SensorSimulationService(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<SensorSimulationService> logger) : ISensorSimulationService
{
    private readonly int _tickCount = config.GetValue("Simulation:TickCount", 12);
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(config.GetValue("Simulation:IntervalSeconds", 5));
    private readonly HashSet<Guid> _running = [];
    private readonly object _lock = new();

    public int TotalDurationSeconds => _tickCount * (int)_interval.TotalSeconds;

    public bool TryStart(Guid batchId)
    {
        lock (_lock)
        {
            if (!_running.Add(batchId)) return false;
        }

        _ = Task.Run(() => RunAsync(batchId));
        return true;
    }

    private async Task RunAsync(Guid batchId)
    {
        try
        {
            var rng = Random.Shared;
            var current = await GetStartingTemperatureAsync(batchId);

            for (var tick = 0; tick < _tickCount; tick++)
            {
                // Random walk suave; a cada 4 ticks aplica um "spike" proposital pra
                // atravessar a faixa ideal (18-30°C) e provar o alerta de fora-da-faixa na UI.
                var delta = (decimal)(rng.NextDouble() * 1.6 - 0.8);
                if (tick > 0 && tick % 4 == 0) delta += rng.Next(2) == 0 ? 3.5m : -3.5m;
                current = Math.Clamp(current + delta, 10m, 40m);

                if (!await RecordTickAsync(batchId, current)) break; // lote sumiu/virou terminal — para

                // O RollupWorker consolida no seu próprio timer (minutos, em prod), que não é
                // garantido bater dentro da janela de 60s do burst — sem isso o gráfico de
                // "Telemetria diária" fica vazio na demo. Consolida aqui a cada poucos ticks
                // pra o front ver o dado quase em tempo real, sem esperar o worker.
                if (tick % 3 == 2) await ConsolidateAsync();

                await Task.Delay(_interval);
            }

            await ConsolidateAsync();
            logger.LogInformation("Simulação concluída para lote {BatchId} ({Ticks} leituras)", batchId, _tickCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha na simulação de sensor pro lote {BatchId}", batchId);
        }
        finally
        {
            lock (_lock) { _running.Remove(batchId); }
        }
    }

    private async Task ConsolidateAsync()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var rollupService = scope.ServiceProvider.GetRequiredService<IRollupService>();
            await rollupService.ConsolidateDataAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Best-effort: se colidir com o RollupWorker rodando no mesmo instante, o próximo
            // tick tenta de novo — não deve derrubar o burst de simulação por causa disso.
            logger.LogWarning(ex, "Consolidação sob demanda falhou, seguindo o burst normalmente");
        }
    }

    private async Task<decimal> GetStartingTemperatureAsync(Guid batchId)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ILimsDbContext>();
        var batch = await db.Batches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == batchId);
        return batch?.CurrentTemperature ?? 22m;
    }

    private async Task<bool> RecordTickAsync(Guid batchId, decimal temperature)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ILimsDbContext>();
        var events = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var batch = await db.Batches.FindAsync(batchId);
        if (batch is null || batch.Status is BatchStatus.Released or BatchStatus.Rejected)
            return false;

        var reading = new SensorData
        {
            BatchId = batchId,
            Temperature = temperature,
            ReadingTime = DateTime.UtcNow,
        };
        db.SensorData.Add(reading);
        batch.CurrentTemperature = temperature;

        if (SensorThresholds.IsOutOfRange(temperature))
        {
            await events.PublishAsync(new SensorReadingOutOfRangeEvent(
                BatchId: batchId,
                ReadingId: reading.Id,
                Temperature: temperature,
                MinThreshold: SensorThresholds.MinTemperatureCelsius,
                MaxThreshold: SensorThresholds.MaxTemperatureCelsius,
                ReadingTime: reading.ReadingTime,
                OccurredAt: DateTime.UtcNow));
        }

        await db.SaveChangesAsync();
        return true;
    }
}
