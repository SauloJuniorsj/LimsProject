using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimsProject.Features.SeedLots;

public sealed class SeedLotConfiguration : IEntityTypeConfiguration<SeedLot>
{
    public void Configure(EntityTypeBuilder<SeedLot> b)
    {
        b.ToTable("SeedLots");
        b.HasKey(x => x.Id);
        b.Property(x => x.Supplier).HasMaxLength(256).IsRequired();
        b.Property(x => x.LotCode).HasMaxLength(128).IsRequired();
        b.HasIndex(x => new { x.StrainId, x.LotCode }).IsUnique();
        b.HasOne<LimsProject.Features.Genetics.Strain>()
            .WithMany()
            .HasForeignKey(x => x.StrainId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
