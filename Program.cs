using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using LimsProject.API.Endpoints;
using LimsProject.API.Middleware;
using LimsProject.Application.Interfaces;
using LimsProject.Application.Observability;
using LimsProject.Application.Services;
using LimsProject.Application.Workers;
using LimsProject.Infrastructure.Auth;
using LimsProject.Infrastructure.HealthChecks;
using LimsProject.Infrastructure.Messaging;
using LimsProject.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure — DbContext
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

// Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(opt =>
    {
        opt.Password.RequireDigit = true;
        opt.Password.RequiredLength = 8;
        opt.Password.RequireUppercase = false;
        opt.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(opt =>
    {
        opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", p => p.RequireRole("Admin"))
    .AddPolicy("LabOrAdmin", p => p.RequireRole("Lab", "Admin"));

// Application
builder.Services.AddScoped<IRollupService, RollupService>();
builder.Services.AddHostedService<RollupWorker>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddSingleton<LimsMetrics>();

// Event publishing — Outbox pattern quando broker está habilitado.
//
// Endpoints injetam IEventPublisher e chamam PublishAsync ANTES do SaveChanges:
//   • OutboxEventPublisher (broker on)  — escreve OutboxMessage na MESMA transação
//   • NullEventPublisher    (broker off / Testing) — no-op
//
// Quando broker on: OutboxRelayWorker polla a tabela e despacha pro RabbitMQ
// via IRabbitMqClient com retry + LastError. Zero dual-write, zero perda em crash.
var rabbitEnabled = builder.Configuration.GetValue("RabbitMq:Enabled", false)
    && !builder.Environment.IsEnvironment("Testing");
if (rabbitEnabled)
{
    builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();
    builder.Services.AddSingleton<IRabbitMqClient, RabbitMqClient>();
    builder.Services.AddHostedService<OutboxRelayWorker>();
}
else
{
    builder.Services.AddScoped<IEventPublisher, NullEventPublisher>();
}

// OpenTelemetry — traces + metrics (desativado em Testing pra não poluir output)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("LimsProject"))
        .WithTracing(t => t
            .AddAspNetCoreInstrumentation()
            .AddSource("LimsProject")
            .AddConsoleExporter())
        .WithMetrics(m => m
            .AddMeter(LimsMetrics.MeterName)
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddConsoleExporter());
}

// API versioning — header `api-version: 1.0` ou query `?api-version=1.0`.
// Sem header → fallback pra v1.0 (zero break em clientes existentes).
// Response carrega `api-supported-versions` automaticamente.
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new HeaderApiVersionReader("api-version"),
        new QueryStringApiVersionReader("api-version"));
});

// Rate limiting — proteção contra brute-force no login
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", o =>
    {
        o.PermitLimit = builder.Configuration.GetValue<int>("RateLimit:LoginPerMinute", 30);
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Problem Details — respostas de erro padronizadas (RFC 7807)
builder.Services.AddProblemDetails();

// IMemoryCache pra GETs read-heavy (batch summary). Invalidação targeted nos writes.
builder.Services.AddMemoryCache();

// Healthcheck — DbContext + RabbitMQ connectivity (se habilitado) + Outbox lag
var healthChecks = builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>()
    .AddCheck<OutboxLagHealthCheck>("outbox-lag", tags: ["outbox"]);
if (rabbitEnabled)
    healthChecks.AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["broker"]);

// API / Swagger com botão de autenticação JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LIMS API",
        Version = "v1",
        Description = "Laboratory Information Management System para empresas de cannabis."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer. Faça login em /auth/login e cole o token aqui: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Auto-migration e seed de roles (apenas fora do ambiente de testes)
if (!app.Environment.IsEnvironment("Testing"))
{
    await using var scope = app.Services.CreateAsyncScope();

    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Lab", "Admin" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}
else
{
    // Em Testing, só semeia as roles (InMemory não precisa de migrate)
    await using var scope = app.Services.CreateAsyncScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Lab", "Admin" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "LIMS API v1"));

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();

// Group versionado v1.0 — adiciona "api-supported-versions: 1.0" em todos os responses
// (clientes sem header `api-version` caem no default v1.0 via AssumeDefaultVersionWhenUnspecified)
var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

var v1 = app.MapGroup("").WithApiVersionSet(versionSet).HasApiVersion(1, 0);
v1.MapAuthEndpoints();
v1.MapUsersEndpoints();
v1.MapBatchEndpoints();
v1.MapSensorDataEndpoints();
v1.MapAnalysisEndpoints();

app.MapDebugEndpoints(); // fora do version set — debug não é parte da API pública

app.Run();

public partial class Program { }
