using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimsProject.Features.Batches;

public sealed class BatchConfiguration : IEntityTypeConfiguration<Batch>
{
    public void Configure(EntityTypeBuilder<Batch> b)
    {
        b.ToTable("Batches");
        b.HasKey(x => x.Id);
        b.Property(x => x.RoomId).HasMaxLength(128);
        b.HasIndex(x => x.SeedLotId);
        b.HasIndex(x => x.Status);
        b.HasOne<LimsProject.Features.SeedLots.SeedLot>()
            .WithMany()
            .HasForeignKey(x => x.SeedLotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
