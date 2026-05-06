using System.Text.Json;
using LimsProject.Common.Persistence;

namespace LimsProject.Features.Traceability;

public sealed class ChainOfCustodyWriter(AppDbContext db) : IChainOfCustodyWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public void Append(Guid batchId, string eventType, object? payload, Guid? operatorId)
    {
        var json = payload is null ? "{}" : JsonSerializer.Serialize(payload, JsonOptions);
        db.ChainOfCustodyEvents.Add(new ChainOfCustodyEvent
        {
            BatchId = batchId,
            EventType = eventType,
            PayloadJson = json,
            OperatorId = operatorId,
            OccurredAt = DateTime.UtcNow,
        });
    }
}
