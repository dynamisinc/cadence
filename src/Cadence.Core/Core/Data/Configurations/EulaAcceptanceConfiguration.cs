using Cadence.Core.Features.SystemSettings.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class EulaAcceptanceConfiguration : IEntityTypeConfiguration<EulaAcceptance>
{
    public void Configure(EntityTypeBuilder<EulaAcceptance> builder)
    {
        builder.ToTable("EulaAcceptances");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UserId).IsRequired().HasMaxLength(450);
        builder.Property(e => e.EulaVersion).IsRequired().HasMaxLength(50);
        builder.HasIndex(e => new { e.UserId, e.EulaVersion }).IsUnique();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
