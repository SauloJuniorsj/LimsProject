using LimsProject.Common.Results;

namespace LimsProject.Features.Batches;

public sealed class BatchTransitionService : IBatchTransitionService
{
    public Result ValidateTransition(BatchStatus from, BatchStatus to)
    {
        if (from == to)
            return Result.Failure("Estado igual ao atual.");

        var ok = (from, to) switch
        {
            (BatchStatus.Germination, BatchStatus.Vegetative) => true,
            (BatchStatus.Vegetative, BatchStatus.Flowering) => true,
            (BatchStatus.Flowering, BatchStatus.Harvested) => true,
            (BatchStatus.Harvested, BatchStatus.Drying) => true,
            (BatchStatus.Drying, BatchStatus.Curing) => true,
            (BatchStatus.Curing, BatchStatus.Testing) => true,
            (BatchStatus.Testing, BatchStatus.Released) => true,
            (BatchStatus.Testing, BatchStatus.Rejected) => true,
            (BatchStatus.Released, BatchStatus.Packaged) => true,
            (BatchStatus.Packaged, BatchStatus.Sold) => true,
            _ => false,
        };

        return ok
            ? Result.Success()
            : Result.Failure($"Transição inválida: {from} → {to}");
    }
}
