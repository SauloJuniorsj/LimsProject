using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimsProject.Features.Plants;

public sealed class PlantConfiguration : IEntityTypeConfiguration<Plant>
{
    public void Configure(EntityTypeBuilder<Plant> b)
    {
        b.ToTable("Plants");
        b.HasKey(x => x.Id);
        b.Property(x => x.TagCode).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.TagCode).IsUnique();
        b.HasIndex(x => x.BatchId);
        b.HasOne<LimsProject.Features.Batches.Batch>()
            .WithMany()
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
