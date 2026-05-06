using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimsProject.Features.LabAnalysis;

public sealed class LabAnalysisConfiguration : IEntityTypeConfiguration<LabAnalysis>
{
    public void Configure(EntityTypeBuilder<LabAnalysis> b)
    {
        b.ToTable("LabAnalyses");
        b.HasKey(x => x.Id);
        b.Property(x => x.Thc).HasColumnName("THC");
        b.Property(x => x.Cbd).HasColumnName("CBD");
        b.Property(x => x.Terpenes).HasMaxLength(1024);
        b.HasIndex(x => x.BatchId);
        b.HasOne<LimsProject.Features.Batches.Batch>()
            .WithMany()
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
