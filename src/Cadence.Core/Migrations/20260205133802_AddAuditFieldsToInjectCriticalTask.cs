using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFieldsToInjectCriticalTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add nullable columns first
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "InjectCriticalTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "InjectCriticalTasks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            // Populate existing records with defaults
            migrationBuilder.Sql(@"
                UPDATE InjectCriticalTasks
                SET CreatedAt = GETUTCDATE(),
                    CreatedBy = 'system-migration'
                WHERE CreatedAt IS NULL
            ");

            // Make columns non-nullable
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "InjectCriticalTasks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "InjectCriticalTasks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "InjectCriticalTasks");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "InjectCriticalTasks");
        }
    }
}
