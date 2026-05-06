using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimsProject.Features.Sensors;

public sealed class SensorDataConfiguration : IEntityTypeConfiguration<SensorData>
{
    public void Configure(EntityTypeBuilder<SensorData> b)
    {
        b.ToTable("SensorData");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.BatchId, x.ReadingTime });
        b.HasOne<LimsProject.Features.Batches.Batch>()
            .WithMany()
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
