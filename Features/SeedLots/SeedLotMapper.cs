namespace LimsProject.Features.SeedLots;

public static class SeedLotMapper
{
    public static SeedLotResponse ToResponse(this SeedLot s) =>
        new(s.Id, s.StrainId, s.Supplier, s.LotCode, s.Quantity, s.ReceivedAt, s.CreatedAt);

    public static SeedLot ToEntity(this CreateSeedLotRequest r) => new()
    {
        StrainId = r.StrainId,
        Supplier = r.Supplier.Trim(),
        LotCode = r.LotCode.Trim(),
        Quantity = r.Quantity,
        ReceivedAt = r.ReceivedAt ?? DateTime.UtcNow,
    };
}
