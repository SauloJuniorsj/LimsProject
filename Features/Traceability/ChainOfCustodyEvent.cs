namespace LimsProject.Features.Traceability;

public sealed class ChainOfCustodyEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid? OperatorId { get; set; }
}
