namespace LimsProject.Features.Packaging;

public sealed record CreateFinishedProductRequest(
    string Name,
    Guid StrainId,
    decimal UnitWeightGrams,
    ProductFormat Format);

public sealed record FinishedProductResponse(
    Guid Id,
    string Name,
    Guid StrainId,
    decimal UnitWeightGrams,
    ProductFormat Format,
    DateTime CreatedAt);

public sealed record PackBatchRequest(
    Guid FinishedProductId,
    int UnitCount,
    decimal UnitWeightGrams);

public sealed record ProductPackageResponse(
    Guid Id,
    string SerialNumber,
    string QrPayload,
    decimal WeightGrams,
    DateTime PackagedAt);
