using LimsProject.Features.Batches;
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

namespace LimsProject.Common.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Strain> Strains => Set<Strain>();
    public DbSet<SeedLot> SeedLots => Set<SeedLot>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<SensorData> SensorData => Set<SensorData>();
    public DbSet<BatchDailySummary> BatchesDailySummaries => Set<BatchDailySummary>();
    public DbSet<AlertThreshold> AlertThresholds => Set<AlertThreshold>();
    public DbSet<EnvironmentalAlert> EnvironmentalAlerts => Set<EnvironmentalAlert>();
    public DbSet<HarvestRecord> HarvestRecords => Set<HarvestRecord>();
    public DbSet<DryingRecord> DryingRecords => Set<DryingRecord>();
    public DbSet<CuringRecord> CuringRecords => Set<CuringRecord>();
    public DbSet<LabAnalysis> LabAnalyses => Set<LabAnalysis>();
    public DbSet<FinishedProduct> FinishedProducts => Set<FinishedProduct>();
    public DbSet<ProductPackage> ProductPackages => Set<ProductPackage>();
    public DbSet<ChainOfCustodyEvent> ChainOfCustodyEvents => Set<ChainOfCustodyEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
