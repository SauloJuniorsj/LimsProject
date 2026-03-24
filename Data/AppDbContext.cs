using LimsProject.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<BatchDailySumarry> BatchesDailySumaries => Set<BatchDailySumarry>();
    public DbSet<SensorData> SensorData=> Set<SensorData>();
    public DbSet<LabAnalysis> LabAnalyses => Set<LabAnalysis>();

}