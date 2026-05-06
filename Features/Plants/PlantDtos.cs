namespace LimsProject.Features.Plants;

public sealed record RegisterPlantRequest(string TagCode, Guid? MotherPlantId);

public sealed record PlantResponse(
    Guid Id,
    Guid BatchId,
    string TagCode,
    PlantStatus Status,
    Guid? MotherPlantId,
    DateTime CreatedAt);

public sealed record UpdatePlantRequest(PlantStatus Status);
