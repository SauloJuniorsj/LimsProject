namespace LimsProject.Features.Plants;

public static class PlantMapper
{
    public static PlantResponse ToResponse(this Plant p) =>
        new(p.Id, p.BatchId, p.TagCode, p.Status, p.MotherPlantId, p.CreatedAt);
}
