using LimsProject.Domain.Enums;

namespace LimsProject.API;

/// <summary>RFC 7807 Problem Details factory — respostas de erro consistentes em toda a API.</summary>
internal static class Problems
{
    public static IResult BatchNotFound() => Results.Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Batch not found",
        detail: "Lote não encontrado.");

    public static IResult InvalidStatusTransition(BatchStatus current, BatchStatus requested, BatchStatus[] allowed) =>
        Results.Problem(
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Invalid status transition",
            detail: $"Transição inválida: {current} → {requested}.",
            extensions: new Dictionary<string, object?>
            {
                ["currentStatus"] = current.ToString(),
                ["requestedStatus"] = requested.ToString(),
                ["allowedTransitions"] = allowed.Select(s => s.ToString()).ToArray()
            });

    public static IResult CannotDeleteBatch(BatchStatus status) => Results.Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Batch cannot be deleted",
        detail: $"Não é possível excluir um lote com status '{status}'.",
        extensions: new Dictionary<string, object?>
        {
            ["currentStatus"] = status.ToString()
        });

    public static IResult SimulationAlreadyRunning() => Results.Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Simulation already running",
        detail: "Já existe uma simulação de sensor em andamento pra este lote.");

    public static IResult BatchTerminal(BatchStatus status) => Results.Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Batch is terminal",
        detail: $"Lote em estado terminal ({status}) não aceita simulação de sensor.",
        extensions: new Dictionary<string, object?>
        {
            ["currentStatus"] = status.ToString()
        });
}
