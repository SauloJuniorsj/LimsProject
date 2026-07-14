using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using LimsProject.Application.Events;
using LimsProject.Application.Interfaces;
using LimsProject.Application.Observability;
using LimsProject.Infrastructure.HealthChecks;
using LimsProject.Infrastructure.Messaging;
using LimsProject.Infrastructure.Persistence;
using LimsProject.Infrastructure.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LimsProject.API;

internal static class StartupExtensions
{
    public static IServiceCollection AddLimsIdentityAndAuth(this IServiceCollection services, IConfiguration config)
    {
        services.AddIdentity<IdentityUser, IdentityRole>(opt =>
            {
                opt.Password.RequireDigit = true;
                opt.Password.RequiredLength = 8;
                opt.Password.RequireUppercase = false;
                opt.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        var jwtKey = config["Jwt:Key"]!;
        services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opt => opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config["Jwt:Issuer"],
                ValidAudience = config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", p => p.RequireRole("Admin"))
            .AddPolicy("LabOrAdmin", p => p.RequireRole("Lab", "Admin"));

        return services;
    }

    // Outbox: quando broker on, OutboxEventPublisher grava na tabela na mesma transação
    // e OutboxRelayWorker despacha pro RabbitMQ. Off / Testing → NullEventPublisher no-op.
    public static IServiceCollection AddLimsEventPublishing(this IServiceCollection services, bool rabbitEnabled)
    {
        if (rabbitEnabled)
        {
            services.AddScoped<IEventPublisher, OutboxEventPublisher>();
            services.AddSingleton<IRabbitMqClient, RabbitMqClient>();
            services.AddHostedService<OutboxRelayWorker>();
        }
        else
        {
            services.AddScoped<IEventPublisher, NullEventPublisher>();
        }
        return services;
    }

    public static IServiceCollection AddLimsObservability(this IServiceCollection services, IHostEnvironment env)
    {
        if (env.IsEnvironment("Testing")) return services;

        services.AddOpenTelemetry()
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

        return services;
    }

    public static IServiceCollection AddLimsApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new HeaderApiVersionReader("api-version"),
                new QueryStringApiVersionReader("api-version"));
        }).AddMvc();
        return services;
    }

    public static IServiceCollection AddLimsRateLimiting(this IServiceCollection services, IConfiguration config)
    {
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("login", o =>
            {
                o.PermitLimit = config.GetValue<int>("RateLimit:LoginPerMinute", 30);
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit = 0;
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
        return services;
    }

    public static IServiceCollection AddLimsHealthChecks(this IServiceCollection services, bool rabbitEnabled)
    {
        var checks = services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>()
            .AddCheck<OutboxLagHealthCheck>("outbox-lag", tags: ["outbox"]);
        if (rabbitEnabled)
            checks.AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["broker"]);
        return services;
    }

    public static IServiceCollection AddLimsSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "LIMS API",
                Version = "v1",
                Description = "Laboratory Information Management System para empresas de cannabis.",
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Bearer. Faça login em /auth/login e cole o token aqui: Bearer {token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                    },
                    Array.Empty<string>()
                },
            });
        });
        return services;
    }

    // Credenciais fixas de demonstração — mesmos valores exibidos na tela de login
    // (web/src/routes/login.tsx). Existem pra qualquer visitante (recrutador incluso)
    // entrar sem precisar de cadastro. Nunca usar esse padrão com dado real em produção.
    private static readonly (string Email, string Password, string Role)[] DemoUsers =
    [
        ("admin@lims.demo", "Demo1234", "Admin"),
        ("lab@lims.demo", "Demo1234", "Lab"),
    ];

    public static async Task EnsureSeededAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();

        if (!app.Environment.IsEnvironment("Testing"))
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Lab", "Admin" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (!app.Environment.IsEnvironment("Testing"))
            await SeedDemoUsersAsync(scope.ServiceProvider);
    }

    private static async Task SeedDemoUsersAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        foreach (var (email, password, role) in DemoUsers)
        {
            if (await userManager.FindByEmailAsync(email) is not null) continue;

            var user = new IdentityUser { Email = email, UserName = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, role);
        }
    }
}
