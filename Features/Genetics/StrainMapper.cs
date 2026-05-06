namespace LimsProject.Features.Genetics;

public static class StrainMapper
{
    public static StrainResponse ToResponse(this Strain s) =>
        new(s.Id, s.Name, s.Type, s.ThcMaxLimit, s.IsHemp, s.CreatedAt);

    public static Strain ToEntity(this CreateStrainRequest r) => new()
    {
        Name = r.Name.Trim(),
        Type = r.Type,
        ThcMaxLimit = r.ThcMaxLimit,
        IsHemp = r.IsHemp,
    };
}
