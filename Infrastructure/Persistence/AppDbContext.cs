using LimsProject.Application.Interfaces;
using LimsProject.Domain.Common;
using LimsProject.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<IdentityUser>, ILimsDbContext
{
    private readonly ICurrentUserService? _currentUser;

    // Constructor curto: usado em testes que instanciam o context direto (RollupServiceTests)
    public AppDbContext(DbContextOptions<AppDbContext> options) : this(options, null) { }

    // Constructor longo: escolhido pelo DI quando ICurrentUserService está registrado
    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService? currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<BatchDailySummary> BatchesDailySummaries => Set<BatchDailySummary>();
    public DbSet<SensorData> SensorData => Set<SensorData>();
    public DbSet<LabAnalysis> LabAnalyses => Set<LabAnalysis>();
    public DbSet<BatchStatusHistory> BatchStatusHistories => Set<BatchStatusHistory>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

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

        // Decimal precision — explícito pra silenciar EF warnings e ter schema previsível
        modelBuilder.Entity<Batch>().Property(b => b.ThcPercentage).HasPrecision(5, 2);
        modelBuilder.Entity<Batch>().Property(b => b.CbdPercentage).HasPrecision(5, 2);
        modelBuilder.Entity<Batch>().Property(b => b.CurrentMoisture).HasPrecision(5, 2);
        modelBuilder.Entity<Batch>().Property(b => b.CurrentTemperature).HasPrecision(5, 2);
        modelBuilder.Entity<Batch>().Property(b => b.AverageTemperature).HasPrecision(5, 2);
        modelBuilder.Entity<SensorData>().Property(s => s.Temperature).HasPrecision(5, 2);
        modelBuilder.Entity<LabAnalysis>().Property(a => a.THC).HasPrecision(5, 2);
        modelBuilder.Entity<LabAnalysis>().Property(a => a.CBD).HasPrecision(5, 2);
        modelBuilder.Entity<BatchDailySummary>().Property(s => s.AvgTemperature).HasPrecision(5, 2);
        modelBuilder.Entity<BatchDailySummary>().Property(s => s.MinTemperature).HasPrecision(5, 2);
        modelBuilder.Entity<BatchDailySummary>().Property(s => s.MaxTemperature).HasPrecision(5, 2);

        // Batch indexes
        modelBuilder.Entity<Batch>().HasIndex(b => b.Strain);
        modelBuilder.Entity<Batch>().HasIndex(b => b.Status);

        // Global query filter: soft-deleted batches são invisíveis pra queries normais.
        // Use IgnoreQueryFilters() pra inspeção (auditoria, restore, etc).
        modelBuilder.Entity<Batch>().HasQueryFilter(b => b.DeletedAt == null);

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

        // RefreshToken: lookup por hash (unique), filtragem por usuário
        modelBuilder.Entity<RefreshToken>().ToTable("RefreshTokens");
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash).IsUnique();
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.UserId);

        // OutboxMessage: o worker polla por (PublishedAt IS NULL ORDER BY CreatedAt) —
        // índice composto cobre isso direto.
        modelBuilder.Entity<OutboxMessage>().ToTable("OutboxMessages");
        modelBuilder.Entity<OutboxMessage>()
            .HasIndex(m => new { m.PublishedAt, m.CreatedAt });
    }

    /// <summary>
    /// Interceptor cross-cutting: preenche IAuditable em Added/Modified e converte
    /// EntityState.Deleted em UPDATE com DeletedAt setado pra entidades ISoftDeletable.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var user = _currentUser?.GetEmail();

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // CreatedAt vem do construtor da entidade (= DateTime.UtcNow no Batch).
                    // Não sobrescrevemos pra permitir seed com backdating.
                    // CreatedBy: só preenche se ninguém setou (??=).
                    if (entry.Entity.CreatedAt == default)
                        entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy ??= user;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = user;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.DeletedAt = now;
                entry.Entity.DeletedBy = user;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
