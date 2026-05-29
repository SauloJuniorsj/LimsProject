using System.Reflection;
using LimsProject.API.Endpoints;
using LimsProject.Application.Interfaces;
using LimsProject.Application.Services;
using LimsProject.Application.Workers;
using LimsProject.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<ILimsDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// Application
builder.Services.AddScoped<IRollupService, RollupService>();
builder.Services.AddHostedService<RollupWorker>();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapBatchEndpoints();
app.MapAnalysisEndpoints();
app.MapDebugEndpoints();

app.Run();
