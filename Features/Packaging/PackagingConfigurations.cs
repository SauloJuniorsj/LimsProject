using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimsProject.Features.Packaging;

public sealed class FinishedProductConfiguration : IEntityTypeConfiguration<FinishedProduct>
{
    public void Configure(EntityTypeBuilder<FinishedProduct> b)
    {
        b.ToTable("FinishedProducts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.HasIndex(x => x.StrainId);
        b.HasOne<LimsProject.Features.Genetics.Strain>()
            .WithMany()
            .HasForeignKey(x => x.StrainId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductPackageConfiguration : IEntityTypeConfiguration<ProductPackage>
{
    public void Configure(EntityTypeBuilder<ProductPackage> b)
    {
        b.ToTable("ProductPackages");
        b.HasKey(x => x.Id);
        b.Property(x => x.SerialNumber).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.SerialNumber).IsUnique();
        b.HasIndex(x => x.BatchId);
        b.HasOne<LimsProject.Features.Batches.Batch>()
            .WithMany()
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<FinishedProduct>()
            .WithMany()
            .HasForeignKey(x => x.FinishedProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
