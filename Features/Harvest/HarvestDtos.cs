namespace LimsProject.Features.Harvest;

public sealed record RegisterHarvestRequest(decimal WetWeightGrams, Guid? OperatorId);

public sealed record HarvestResponse(
    Guid Id,
    Guid BatchId,
    DateTime HarvestDate,
    decimal WetWeightGrams,
    Guid? OperatorId);
