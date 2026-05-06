using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimsProject.Features.Sensors.Rollup;

public sealed class BatchDailySummaryConfiguration : IEntityTypeConfiguration<BatchDailySummary>
{
    public void Configure(EntityTypeBuilder<BatchDailySummary> b)
    {
        b.ToTable("BatchesDailySumaries");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.BatchId, x.Date }).IsUnique();
        b.HasOne<LimsProject.Features.Batches.Batch>()
            .WithMany()
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
