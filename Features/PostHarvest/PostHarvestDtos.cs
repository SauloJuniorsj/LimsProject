namespace LimsProject.Features.PostHarvest;

public sealed record CompleteDryingRequest(decimal DryWeightGrams);

public sealed record CompleteCuringRequest(decimal FinalMoisture);
