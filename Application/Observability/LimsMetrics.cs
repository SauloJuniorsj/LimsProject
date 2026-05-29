using System.Diagnostics.Metrics;

namespace LimsProject.Application.Observability;

/// <summary>Métricas de domínio expostas via OpenTelemetry.</summary>
public class LimsMetrics
{
    public const string MeterName = "LimsProject";

    private readonly Counter<long> _batchesCreated;
    private readonly Counter<long> _analysesCompleted;
    private readonly Counter<long> _statusTransitions;

    public LimsMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _batchesCreated = meter.CreateCounter<long>(
            "lims.batches.created",
            unit: "{batch}",
            description: "Quantidade de lotes criados.");

        _analysesCompleted = meter.CreateCounter<long>(
            "lims.analyses.completed",
            unit: "{analysis}",
            description: "Análises laboratoriais finalizadas, marcadas com tag passed=true|false.");

        _statusTransitions = meter.CreateCounter<long>(
            "lims.status.transitions",
            unit: "{transition}",
            description: "Mudanças de status de lote, marcadas com from/to.");
    }

    public void BatchCreated() => _batchesCreated.Add(1);

    public void AnalysisCompleted(bool passed) =>
        _analysesCompleted.Add(1, new KeyValuePair<string, object?>("passed", passed));

    public void StatusTransition(string from, string to) =>
        _statusTransitions.Add(1,
            new KeyValuePair<string, object?>("from", from),
            new KeyValuePair<string, object?>("to", to));
}
