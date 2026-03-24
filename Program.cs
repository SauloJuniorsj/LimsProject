using Bogus;
using FluentValidation;
using LimsProject.Models;
using LimsProject.Services;
using LimsProject.Validators;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql("Host=localhost;Database=lims_db;Username=user;Password=password"));
builder.Services.AddHostedService<RollupWorker>();

// Isso registra TODOS os validadores que estiverem na pasta Validators (ou no projeto todo)
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IRollupService, RollupService>();
builder.Services.AddHostedService<RollupWorker>();
var app = builder.Build();

// Criar novo Lote (Seed-to-Sale)
app.MapPost("/batches", async (Batch batch, AppDbContext db) => {
    db.Batches.Add(batch);
    await db.SaveChangesAsync();
    return Results.Created($"/batches/{batch.Id}", batch);
});

// Endpoint para o Dashboard (Consome a tabela de Rollup, não a bruta)
app.MapGet("/batches/{id}/summary", async (Guid id, AppDbContext db) => {
    return await db.Batches
        .Where(s => s.Id == id)
        .ToListAsync();
});

app.MapPost("/debug/populate-elegant", async (AppDbContext db) =>
{
    // Configura o gerador de "Mentiras" (Bogus)
    var batchFaker = new Faker<Batch>()
        .RuleFor(b => b.Id, f => Guid.NewGuid())
        .RuleFor(b => b.Strain, f => f.PickRandom("Purple Basil", "Dill", "Mint", "White Widow"))
        .RuleFor(b => b.Status, f => BatchStatus.Growth);

    var batches = batchFaker.Generate(3); // Cria 3 lotes
    db.Batches.AddRange(batches);

    foreach (var b in batches)
    {
        var sensorFaker = new Faker<SensorData>()
            .RuleFor(s => s.Id, f => Guid.NewGuid())
            .RuleFor(s => s.BatchId, b.Id)
            .RuleFor(s => s.Temperature, f => f.Finance.Amount(15, 35)) // Gera temp entre 15 e 35
            .RuleFor(s => s.ReadingTime, f => f.Date.Recent(1).ToUniversalTime()); // Dados das últimas 24h

        db.SensorData.AddRange(sensorFaker.Generate(20));
    }

    await db.SaveChangesAsync();
    return Results.Ok("3 Lotes e 60 Logs gerados com Bogus!");
});

app.MapPost("/batches/{id}/analysis", async (Guid id, LabAnalysis analysis, AppDbContext db, IValidator<LabAnalysis> validator) =>
{
    // 1. Vincula ao ID da URL
    analysis.BatchId = id;
    analysis.AnalysisDate = DateTime.UtcNow;

    // 2. Valida
    var validationResult = await validator.ValidateAsync(analysis);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

    // 3. Verifica se o Lote existe
    var batch = await db.Batches.FindAsync(id);
    if (batch == null) return Results.NotFound("Lote não encontrado.");

    // 4. Salva a análise e atualiza o status do lote
    db.LabAnalyses.Add(analysis);
    batch.Status = analysis.IsPassed ? BatchStatus.Released : BatchStatus.Rejected;

    await db.SaveChangesAsync();
    return Results.Created($"/analysis/{analysis.Id}", analysis);
});

app.UseSwagger();
app.UseSwaggerUI();

app.Run();