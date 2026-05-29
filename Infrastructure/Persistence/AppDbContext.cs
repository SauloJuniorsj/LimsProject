using LimsProject.Application.Interfaces;
using LimsProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Infrastructure.Persistence;

public class AppDbContext : DbContext, ILimsDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<BatchDailySummary> BatchesDailySummaries => Set<BatchDailySummary>();
    public DbSet<SensorData> SensorData => Set<SensorData>();
    public DbSet<LabAnalysis> LabAnalyses => Set<LabAnalysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Preserva os nomes de tabela existentes no banco
        modelBuilder.Entity<Batch>().ToTable("Batches");
        modelBuilder.Entity<BatchDailySummary>().ToTable("BatchesDailySumaries");
        modelBuilder.Entity<LabAnalysis>().ToTable("LabAnalyses");
        modelBuilder.Entity<SensorData>().ToTable("SensorData");

        // Preserva o nome de coluna existente (corrigimos o typo só no C#)
        modelBuilder.Entity<Batch>()
            .Property(b => b.AverageTemperature)
            .HasColumnName("AvarageTemperature");
    }
}
