namespace LimsProject.Features.Traceability;

public static class CocEventTypes
{
    public const string BatchCreated = "batch.created";
    public const string BatchTransition = "batch.transition";
    public const string PlantRegistered = "plant.registered";
    public const string PlantUpdated = "plant.updated";
    public const string SensorsIngested = "sensors.ingested";
    public const string AlertRaised = "alert.raised";
    public const string HarvestRegistered = "harvest.registered";
    public const string DryingStarted = "drying.started";
    public const string DryingCompleted = "drying.completed";
    public const string CuringStarted = "curing.started";
    public const string CuringCompleted = "curing.completed";
    public const string LabAnalysisSubmitted = "lab.analysis.submitted";
    public const string PackagingCreated = "packaging.created";
    public const string PackageSold = "package.sold";
}
