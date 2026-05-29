using Bogus;
using LimsProject.Application.Interfaces;
using LimsProject.Domain.Entities;
using LimsProject.Domain.Enums;

namespace LimsProject.API.Endpoints;

public static class DebugEndpoints
{
    public static void MapDebugEndpoints(this WebApplication app)
    {
        app.MapPost("/debug/populate-elegant", async (ILimsDbContext db) =>
        {
            var batchFaker = new Faker<Batch>()
                .RuleFor(b => b.Id, _ => Guid.NewGuid())
                .RuleFor(b => b.Strain, f => f.PickRandom("Purple Basil", "Dill", "Mint", "White Widow"))
                .RuleFor(b => b.Status, _ => BatchStatus.Growth);

            var batches = batchFaker.Generate(3);
            db.Batches.AddRange(batches);

            foreach (var batch in batches)
            {
                var sensorFaker = new Faker<SensorData>()
                    .RuleFor(s => s.Id, _ => Guid.NewGuid())
                    .RuleFor(s => s.BatchId, _ => batch.Id)
                    .RuleFor(s => s.Temperature, f => f.Finance.Amount(15, 35))
                    .RuleFor(s => s.ReadingTime, f => f.Date.Recent(1).ToUniversalTime());

                db.SensorData.AddRange(sensorFaker.Generate(20));
            }

            await db.SaveChangesAsync();
            return Results.Ok("3 Lotes e 60 Logs gerados com Bogus!");
        }).RequireAuthorization("AdminOnly");
    }
}
