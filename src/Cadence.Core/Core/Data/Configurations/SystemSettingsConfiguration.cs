using Cadence.Core.Features.SystemSettings.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
{
    public void Configure(EntityTypeBuilder<SystemSettings> builder)
    {
        builder.ToTable("SystemSettings");
        builder.Property(e => e.SupportAddress).HasMaxLength(200);
        builder.Property(e => e.DefaultSenderAddress).HasMaxLength(200);
        builder.Property(e => e.DefaultSenderName).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(450);
        builder.Property(e => e.EulaVersion).HasMaxLength(50);
    }
}
