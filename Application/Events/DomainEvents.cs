using LimsProject.Domain.Enums;

namespace LimsProject.Application.Events;

public record BatchCreatedEvent(
    Guid BatchId,
    string Strain,
    DateTime OccurredAt
);

public record BatchStatusChangedEvent(
    Guid BatchId,
    BatchStatus? FromStatus,
    BatchStatus ToStatus,
    string ChangedBy,
    string? Reason,
    DateTime OccurredAt
);

public record AnalysisCompletedEvent(
    Guid BatchId,
    Guid AnalysisId,
    decimal Thc,
    decimal Cbd,
    bool Passed,
    DateTime OccurredAt
);
