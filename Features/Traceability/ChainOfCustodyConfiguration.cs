using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimsProject.Features.Traceability;

public sealed class ChainOfCustodyConfiguration : IEntityTypeConfiguration<ChainOfCustodyEvent>
{
    public void Configure(EntityTypeBuilder<ChainOfCustodyEvent> b)
    {
        b.ToTable("ChainOfCustodyEvents");
        b.HasKey(x => x.Id);
        b.Property(x => x.EventType).HasMaxLength(128).IsRequired();
        b.Property(x => x.PayloadJson).HasColumnType("jsonb");
        b.HasIndex(x => new { x.BatchId, x.OccurredAt });
        b.HasOne<LimsProject.Features.Batches.Batch>()
            .WithMany()
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
