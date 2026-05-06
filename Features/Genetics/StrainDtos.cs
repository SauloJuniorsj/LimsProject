namespace LimsProject.Features.Genetics;

public sealed record CreateStrainRequest(
    string Name,
    StrainType Type,
    decimal ThcMaxLimit,
    bool IsHemp);

public sealed record StrainResponse(
    Guid Id,
    string Name,
    StrainType Type,
    decimal ThcMaxLimit,
    bool IsHemp,
    DateTime CreatedAt);
