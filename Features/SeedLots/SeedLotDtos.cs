namespace LimsProject.Features.SeedLots;

public sealed record CreateSeedLotRequest(
    Guid StrainId,
    string Supplier,
    string LotCode,
    int Quantity,
    DateTime? ReceivedAt);

public sealed record SeedLotResponse(
    Guid Id,
    Guid StrainId,
    string Supplier,
    string LotCode,
    int Quantity,
    DateTime ReceivedAt,
    DateTime CreatedAt);
