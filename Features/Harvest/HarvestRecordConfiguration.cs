using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimsProject.Features.Harvest;

public sealed class HarvestRecordConfiguration : IEntityTypeConfiguration<HarvestRecord>
{
    public void Configure(EntityTypeBuilder<HarvestRecord> b)
    {
        b.ToTable("HarvestRecords");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.BatchId);
        b.HasOne<LimsProject.Features.Batches.Batch>()
            .WithMany()
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
