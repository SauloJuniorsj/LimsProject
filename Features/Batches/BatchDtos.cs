namespace LimsProject.Features.Batches;

public sealed record CreateBatchRequest(Guid SeedLotId, string? RoomId);

public sealed record BatchResponse(
    Guid Id,
    Guid SeedLotId,
    string? RoomId,
    BatchStatus Status,
    DateTime CreatedAt);

public sealed record TransitionBatchRequest(BatchStatus Target, string? Reason);

public sealed record BatchSummaryResponse(
    Guid BatchId,
    BatchStatus Status,
    decimal? LatestTemperature,
    decimal? AvgTemperatureLast7Days,
    int ActivePlantCount);
