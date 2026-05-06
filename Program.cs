using FluentValidation;
using LimsProject.Common.Persistence;
using LimsProject.Features.Batches;
using LimsProject.Features.Debug;
using LimsProject.Features.Genetics;
using LimsProject.Features.Harvest;
using LimsProject.Features.LabAnalysis;
using LimsProject.Features.Packaging;
using LimsProject.Features.Plants;
using LimsProject.Features.PostHarvest;
using LimsProject.Features.SeedLots;
using LimsProject.Features.Sensors;
using LimsProject.Features.Sensors.Alerts;
using LimsProject.Features.Sensors.Rollup;
using LimsProject.Features.Traceability;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")
                  ?? "Host=localhost;Database=lims_db;Username=user;Password=password"));

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IStrainService, StrainService>();
builder.Services.AddScoped<ISeedLotService, SeedLotService>();
builder.Services.AddSingleton<IBatchTransitionService, BatchTransitionService>();
builder.Services.AddScoped<IBatchService, BatchService>();
builder.Services.AddScoped<IPlantService, PlantService>();
builder.Services.AddScoped<ISensorIngestionService, SensorIngestionService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IAlertThresholdService, AlertThresholdService>();
builder.Services.AddScoped<IRollupService, RollupService>();
builder.Services.AddScoped<IHarvestService, HarvestService>();
builder.Services.AddScoped<IPostHarvestService, PostHarvestService>();
builder.Services.AddScoped<ILabAnalysisService, LabAnalysisService>();
builder.Services.AddScoped<IPackagingService, PackagingService>();
builder.Services.AddScoped<ITraceabilityService, TraceabilityService>();
builder.Services.AddScoped<IChainOfCustodyWriter, ChainOfCustodyWriter>();

builder.Services.AddHostedService<RollupWorker>();

var app = builder.Build();

app.MapStrainEndpoints();
app.MapSeedLotEndpoints();
app.MapBatchEndpoints();
app.MapPlantEndpoints();
app.MapSensorEndpoints();
app.MapHarvestEndpoints();
app.MapPostHarvestEndpoints();
app.MapLabAnalysisEndpoints();
app.MapPackagingEndpoints();
app.MapTraceabilityEndpoints();
app.MapDebugEndpoints();

app.UseSwagger();
app.UseSwaggerUI();

app.Run();
