namespace LimsProject.Application.Caching;

/// <summary>
/// Catálogo central de chaves de cache. Evita strings espalhadas e garante que
/// invalidação na escrita usa a MESMA chave da leitura.
/// </summary>
public static class CacheKeys
{
    public static string BatchSummary(Guid batchId) => $"batch:{batchId}:summary";
}
