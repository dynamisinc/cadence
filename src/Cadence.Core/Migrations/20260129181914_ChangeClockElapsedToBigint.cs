using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class ChangeClockElapsedToBigint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add a new bigint column
            migrationBuilder.AddColumn<long>(
                name: "ClockElapsedBeforePause_New",
                table: "Exercises",
                type: "bigint",
                nullable: true);

            // Step 2: Convert existing time values to ticks
            // SQL Server time is stored as 100-nanosecond intervals (same as .NET ticks)
            migrationBuilder.Sql(@"
                UPDATE Exercises
                SET ClockElapsedBeforePause_New = CAST(DATEDIFF_BIG(NANOSECOND, '00:00:00', ClockElapsedBeforePause) / 100 AS bigint)
                WHERE ClockElapsedBeforePause IS NOT NULL");

            // Step 3: Drop the old column
            migrationBuilder.DropColumn(
                name: "ClockElapsedBeforePause",
                table: "Exercises");

            // Step 4: Rename the new column
            migrationBuilder.RenameColumn(
                name: "ClockElapsedBeforePause_New",
                table: "Exercises",
                newName: "ClockElapsedBeforePause");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // WARNING: Rolling back will truncate durations > 24 hours to max time value
            migrationBuilder.AddColumn<TimeSpan>(
                name: "ClockElapsedBeforePause_Old",
                table: "Exercises",
                type: "time",
                nullable: true);

            // Convert ticks back to time (capped at 23:59:59.9999999)
            migrationBuilder.Sql(@"
                UPDATE Exercises
                SET ClockElapsedBeforePause_Old =
                    CASE
                        WHEN ClockElapsedBeforePause IS NULL THEN NULL
                        WHEN ClockElapsedBeforePause >= 863999999999 THEN '23:59:59.9999999'
                        ELSE DATEADD(NANOSECOND, ClockElapsedBeforePause * 100, '00:00:00')
                    END");

            migrationBuilder.DropColumn(
                name: "ClockElapsedBeforePause",
                table: "Exercises");

            migrationBuilder.RenameColumn(
                name: "ClockElapsedBeforePause_Old",
                table: "Exercises",
                newName: "ClockElapsedBeforePause");
        }
    }
}
