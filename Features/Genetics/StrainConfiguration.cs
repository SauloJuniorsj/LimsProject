using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimsProject.Features.Genetics;

public sealed class StrainConfiguration : IEntityTypeConfiguration<Strain>
{
    public void Configure(EntityTypeBuilder<Strain> b)
    {
        b.ToTable("Strains");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.HasIndex(x => x.Name);
    }
}
