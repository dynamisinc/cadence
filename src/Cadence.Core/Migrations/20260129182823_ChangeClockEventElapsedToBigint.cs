using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class ChangeClockEventElapsedToBigint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add a new bigint column
            migrationBuilder.AddColumn<long>(
                name: "ElapsedTimeAtEvent_New",
                table: "ClockEvents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // Step 2: Convert existing time values to ticks
            migrationBuilder.Sql(@"
                UPDATE ClockEvents
                SET ElapsedTimeAtEvent_New = CAST(DATEDIFF_BIG(NANOSECOND, '00:00:00', ElapsedTimeAtEvent) / 100 AS bigint)");

            // Step 3: Drop the old column
            migrationBuilder.DropColumn(
                name: "ElapsedTimeAtEvent",
                table: "ClockEvents");

            // Step 4: Rename the new column
            migrationBuilder.RenameColumn(
                name: "ElapsedTimeAtEvent_New",
                table: "ClockEvents",
                newName: "ElapsedTimeAtEvent");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // WARNING: Rolling back will truncate durations > 24 hours
            migrationBuilder.AddColumn<TimeSpan>(
                name: "ElapsedTimeAtEvent_Old",
                table: "ClockEvents",
                type: "time",
                nullable: false,
                defaultValue: TimeSpan.Zero);

            migrationBuilder.Sql(@"
                UPDATE ClockEvents
                SET ElapsedTimeAtEvent_Old =
                    CASE
                        WHEN ElapsedTimeAtEvent >= 863999999999 THEN '23:59:59.9999999'
                        ELSE DATEADD(NANOSECOND, ElapsedTimeAtEvent * 100, '00:00:00')
                    END");

            migrationBuilder.DropColumn(
                name: "ElapsedTimeAtEvent",
                table: "ClockEvents");

            migrationBuilder.RenameColumn(
                name: "ElapsedTimeAtEvent_Old",
                table: "ClockEvents",
                newName: "ElapsedTimeAtEvent");
        }
    }
}
