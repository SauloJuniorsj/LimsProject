using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimsProject.Features.Sensors.Alerts;

public sealed class AlertThresholdConfiguration : IEntityTypeConfiguration<AlertThreshold>
{
    public void Configure(EntityTypeBuilder<AlertThreshold> b)
    {
        b.ToTable("AlertThresholds");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.StrainId);
        b.HasOne<LimsProject.Features.Genetics.Strain>()
            .WithMany()
            .HasForeignKey(x => x.StrainId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class EnvironmentalAlertConfiguration : IEntityTypeConfiguration<EnvironmentalAlert>
{
    public void Configure(EntityTypeBuilder<EnvironmentalAlert> b)
    {
        b.ToTable("EnvironmentalAlerts");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.BatchId, x.Resolved });
        b.HasOne<LimsProject.Features.Batches.Batch>()
            .WithMany()
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
