namespace LimsProject.Features.Batches;

public static class BatchMapper
{
    public static BatchResponse ToResponse(this Batch b) =>
        new(b.Id, b.SeedLotId, b.RoomId, b.Status, b.CreatedAt);

    public static Batch ToEntity(this CreateBatchRequest r) => new()
    {
        SeedLotId = r.SeedLotId,
        RoomId = string.IsNullOrWhiteSpace(r.RoomId) ? null : r.RoomId.Trim(),
        Status = BatchStatus.Germination,
    };
}
