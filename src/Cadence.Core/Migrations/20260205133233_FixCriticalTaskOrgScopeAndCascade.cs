using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class FixCriticalTaskOrgScopeAndCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix cascade cycle issue with InjectCriticalTasks
            migrationBuilder.DropForeignKey(
                name: "FK_InjectCriticalTasks_CriticalTasks_CriticalTaskId",
                table: "InjectCriticalTasks");

            // Step 1: Add nullable OrganizationId column first
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "CriticalTasks",
                type: "uniqueidentifier",
                nullable: true);

            // Step 2: Populate OrganizationId from parent CapabilityTarget for existing records
            migrationBuilder.Sql(@"
                UPDATE ct
                SET ct.OrganizationId = cap.OrganizationId
                FROM CriticalTasks ct
                INNER JOIN CapabilityTargets cap ON ct.CapabilityTargetId = cap.Id
                WHERE ct.OrganizationId IS NULL
            ");

            // Step 3: Make column non-nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationId",
                table: "CriticalTasks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Step 4: Add index and foreign keys
            migrationBuilder.CreateIndex(
                name: "IX_CriticalTasks_OrganizationId",
                table: "CriticalTasks",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_CriticalTasks_Organizations_OrganizationId",
                table: "CriticalTasks",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Re-add InjectCriticalTasks FK with Restrict to avoid cascade cycle
            migrationBuilder.AddForeignKey(
                name: "FK_InjectCriticalTasks_CriticalTasks_CriticalTaskId",
                table: "InjectCriticalTasks",
                column: "CriticalTaskId",
                principalTable: "CriticalTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CriticalTasks_Organizations_OrganizationId",
                table: "CriticalTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_InjectCriticalTasks_CriticalTasks_CriticalTaskId",
                table: "InjectCriticalTasks");

            migrationBuilder.DropIndex(
                name: "IX_CriticalTasks_OrganizationId",
                table: "CriticalTasks");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "CriticalTasks");

            migrationBuilder.AddForeignKey(
                name: "FK_InjectCriticalTasks_CriticalTasks_CriticalTaskId",
                table: "InjectCriticalTasks",
                column: "CriticalTaskId",
                principalTable: "CriticalTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
