using LimsProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Application.Interfaces;

public interface ILimsDbContext
{
    DbSet<Batch> Batches { get; }
    DbSet<SensorData> SensorData { get; }
    DbSet<BatchDailySummary> BatchesDailySummaries { get; }
    DbSet<LabAnalysis> LabAnalyses { get; }
    DbSet<BatchStatusHistory> BatchStatusHistories { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
