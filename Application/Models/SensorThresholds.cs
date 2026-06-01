namespace LimsProject.Application.Models;

/// <summary>
/// Faixas aceitáveis de temperatura pra cultivo de cannabis. Leituras fora dessa janela
/// disparam SensorReadingOutOfRangeEvent via outbox. Configurável via env var no futuro.
/// </summary>
public static class SensorThresholds
{
    public const decimal MinTemperatureCelsius = 18m;
    public const decimal MaxTemperatureCelsius = 30m;

    public static bool IsOutOfRange(decimal temperature) =>
        temperature < MinTemperatureCelsius || temperature > MaxTemperatureCelsius;
}
