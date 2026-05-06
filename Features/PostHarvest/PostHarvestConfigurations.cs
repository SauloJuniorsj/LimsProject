using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimsProject.Features.PostHarvest;

public sealed class DryingRecordConfiguration : IEntityTypeConfiguration<DryingRecord>
{
    public void Configure(EntityTypeBuilder<DryingRecord> b)
    {
        b.ToTable("DryingRecords");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.HarvestId);
        b.HasOne<LimsProject.Features.Harvest.HarvestRecord>()
            .WithMany()
            .HasForeignKey(x => x.HarvestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CuringRecordConfiguration : IEntityTypeConfiguration<CuringRecord>
{
    public void Configure(EntityTypeBuilder<CuringRecord> b)
    {
        b.ToTable("CuringRecords");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.DryingId);
        b.HasOne<DryingRecord>()
            .WithMany()
            .HasForeignKey(x => x.DryingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
