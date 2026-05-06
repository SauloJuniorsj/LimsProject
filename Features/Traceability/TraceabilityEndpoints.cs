namespace LimsProject.Features.Traceability;

public static class TraceabilityEndpoints
{
    public static IEndpointRouteBuilder MapTraceabilityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/trace/{serial}", async (string serial, ITraceabilityService svc, CancellationToken ct) =>
        {
            var trace = await svc.GetTraceBySerialAsync(serial, ct);
            return trace is null ? Results.NotFound() : Results.Ok(trace);
        });

        app.MapGet("/batches/{batchId:guid}/coa", async (Guid batchId, ITraceabilityService svc, CancellationToken ct) =>
        {
            var coa = await svc.GetCertificateAsync(batchId, ct);
            return coa is null ? Results.NotFound() : Results.Ok(coa);
        });

        return app;
    }
}
