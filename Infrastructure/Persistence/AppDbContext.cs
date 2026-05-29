using LimsProject.Application.Interfaces;
using LimsProject.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<IdentityUser>, ILimsDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<BatchDailySummary> BatchesDailySummaries => Set<BatchDailySummary>();
    public DbSet<SensorData> SensorData => Set<SensorData>();
    public DbSet<LabAnalysis> LabAnalyses => Set<LabAnalysis>();
    public DbSet<BatchStatusHistory> BatchStatusHistories => Set<BatchStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // required for Identity tables

        // Preserve existing table/column names for backwards compatibility
        modelBuilder.Entity<Batch>().ToTable("Batches");
        modelBuilder.Entity<BatchDailySummary>().ToTable("BatchesDailySumaries");
        modelBuilder.Entity<LabAnalysis>().ToTable("LabAnalyses");
        modelBuilder.Entity<SensorData>().ToTable("SensorData");

        modelBuilder.Entity<Batch>()
            .Property(b => b.AverageTemperature)
            .HasColumnName("AvarageTemperature");

        // Batch indexes
        modelBuilder.Entity<Batch>().HasIndex(b => b.Strain);
        modelBuilder.Entity<Batch>().HasIndex(b => b.Status);

        // SensorData: FK + composite index for time-range queries per batch
        modelBuilder.Entity<SensorData>()
            .HasOne<Batch>().WithMany().HasForeignKey(s => s.BatchId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SensorData>()
            .HasIndex(s => new { s.BatchId, s.ReadingTime });

        // LabAnalysis: FK + index
        modelBuilder.Entity<LabAnalysis>()
            .HasOne<Batch>().WithMany().HasForeignKey(a => a.BatchId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LabAnalysis>()
            .HasIndex(a => a.BatchId);

        // BatchDailySummary: FK + unique constraint (one summary per batch per day)
        modelBuilder.Entity<BatchDailySummary>()
            .HasOne<Batch>().WithMany().HasForeignKey(s => s.BatchId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<BatchDailySummary>()
            .HasIndex(s => new { s.BatchId, s.Date }).IsUnique();

        // BatchStatusHistory: FK + index para consulta cronológica
        modelBuilder.Entity<BatchStatusHistory>().ToTable("BatchStatusHistories");
        modelBuilder.Entity<BatchStatusHistory>()
            .HasOne<Batch>().WithMany().HasForeignKey(h => h.BatchId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<BatchStatusHistory>()
            .HasIndex(h => new { h.BatchId, h.ChangedAt });
    }
}
