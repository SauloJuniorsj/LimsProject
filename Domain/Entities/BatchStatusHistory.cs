using LimsProject.Domain.Enums;

namespace LimsProject.Domain.Entities;

public class BatchStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public BatchStatus? FromStatus { get; set; }
    public BatchStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string ChangedBy { get; set; } = "system";
    public string? Reason { get; set; }
}
