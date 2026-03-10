using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class HseepRoleConfiguration : IEntityTypeConfiguration<HseepRole>
{
    public void Configure(EntityTypeBuilder<HseepRole> builder)
    {
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasIndex(e => e.Code).IsUnique();

        // Seed HSEEP-defined roles
        builder.HasData(
            new HseepRole
            {
                Id = (int)ExerciseRole.Administrator,
                Code = nameof(ExerciseRole.Administrator),
                Name = "Administrator",
                Description = "System-wide configuration and user management. Has full access to all exercises within their organization.",
                SortOrder = 1,
                IsSystemWide = true,
                CanFireInjects = true,
                CanRecordObservations = true,
                CanManageExercise = true,
                IsActive = true
            },
            new HseepRole
            {
                Id = (int)ExerciseRole.ExerciseDirector,
                Code = nameof(ExerciseRole.ExerciseDirector),
                Name = "Exercise Director",
                Description = "Full exercise management authority. Responsible for Go/No-Go decisions and overall exercise conduct.",
                SortOrder = 2,
                IsSystemWide = false,
                CanFireInjects = true,
                CanRecordObservations = true,
                CanManageExercise = true,
                IsActive = true
            },
            new HseepRole
            {
                Id = (int)ExerciseRole.Controller,
                Code = nameof(ExerciseRole.Controller),
                Name = "Controller",
                Description = "Delivers injects to players and manages scenario flow during exercise conduct.",
                SortOrder = 3,
                IsSystemWide = false,
                CanFireInjects = true,
                CanRecordObservations = false,
                CanManageExercise = false,
                IsActive = true
            },
            new HseepRole
            {
                Id = (int)ExerciseRole.Evaluator,
                Code = nameof(ExerciseRole.Evaluator),
                Name = "Evaluator",
                Description = "Observes and documents player performance for the After-Action Report (AAR).",
                SortOrder = 4,
                IsSystemWide = false,
                CanFireInjects = false,
                CanRecordObservations = true,
                CanManageExercise = false,
                IsActive = true
            },
            new HseepRole
            {
                Id = (int)ExerciseRole.Observer,
                Code = nameof(ExerciseRole.Observer),
                Name = "Observer",
                Description = "Watches exercise conduct without interfering. Read-only access to exercise data.",
                SortOrder = 5,
                IsSystemWide = false,
                CanFireInjects = false,
                CanRecordObservations = false,
                CanManageExercise = false,
                IsActive = true
            }
        );
    }
}
