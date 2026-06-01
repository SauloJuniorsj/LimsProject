namespace LimsProject.API.Middleware;

/// <summary>
/// Garante um Correlation ID por request: lê o header X-Correlation-Id se vier do cliente
/// (útil pra distributed tracing entre serviços), ou gera um novo Guid. Em ambos os casos:
/// - Adiciona ao response header pra o cliente conseguir referenciar
/// - Substitui o HttpContext.TraceIdentifier (vira o ID que o ASP.NET Core usa)
/// - Empurra como log scope pra TODA linha de log do request carregar o CorrelationId
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
