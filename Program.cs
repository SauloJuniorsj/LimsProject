using System.Reflection;
using LimsProject.API;
using LimsProject.API.Middleware;
using LimsProject.Application.Interfaces;
using LimsProject.Application.Observability;
using LimsProject.Application.Services;
using LimsProject.Application.Workers;
using LimsProject.Infrastructure.Auth;
using LimsProject.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure — DbContext (PostgreSQL fora de Testing; tests usam InMemory via factory)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
}
builder.Services.AddScoped<ILimsDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// Identity do usuário atual — capturada por interceptor de SaveChangesAsync pra
// preencher CreatedBy/UpdatedBy/DeletedBy automaticamente em entidades auditáveis.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Identity + JWT + policies (AdminOnly, LabOrAdmin)
builder.Services.AddLimsIdentityAndAuth(builder.Configuration);

// Application services + validators
builder.Services.AddScoped<IRollupService, RollupService>();
builder.Services.AddHostedService<RollupWorker>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddSingleton<LimsMetrics>();

// Event publishing — outbox pattern (RabbitMQ on) ou no-op (off / Testing)
var rabbitEnabled = builder.Configuration.GetValue("RabbitMq:Enabled", false)
    && !builder.Environment.IsEnvironment("Testing");
builder.Services.AddLimsEventPublishing(rabbitEnabled);

// Observability, versioning, controllers, rate limit, errors, cache
builder.Services.AddLimsObservability(builder.Environment);
builder.Services.AddLimsApiVersioning();
builder.Services.AddControllers();
builder.Services.AddLimsRateLimiting(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddMemoryCache();
builder.Services.AddLimsHealthChecks(rabbitEnabled);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddLimsSwagger();

var app = builder.Build();

// Auto-migration (skip em Testing) + seed de roles
await app.EnsureSeededAsync();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "LIMS API v1"));

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();

// Controllers: versioning declarado via [ApiVersion] attribute em cada controller.
// DebugController fica fora da versão pública via [ApiExplorerSettings(IgnoreApi = true)].
app.MapControllers();

app.Run();

public partial class Program { }
