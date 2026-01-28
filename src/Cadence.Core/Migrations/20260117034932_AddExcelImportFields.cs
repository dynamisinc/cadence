using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddExcelImportFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeliveryMethodId",
                table: "Injects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryMethodOther",
                table: "Injects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationName",
                table: "Injects",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationType",
                table: "Injects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Injects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleController",
                table: "Injects",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceReference",
                table: "Injects",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Track",
                table: "Injects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriggerType",
                table: "Injects",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DeliveryMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsOther = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryMethods_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpectedOutcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    WasAchieved = table.Column<bool>(type: "bit", nullable: true),
                    EvaluatorNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpectedOutcomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpectedOutcomes_Injects_InjectId",
                        column: x => x.InjectId,
                        principalTable: "Injects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DeliveryMethods",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "IsOther", "ModifiedBy", "Name", "OrganizationId", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000001"), null, null, "Spoken directly to player", true, false, false, new Guid("00000000-0000-0000-0000-000000000001"), "Verbal", null, 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000002"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000001"), null, null, "Simulated phone call", true, false, false, new Guid("00000000-0000-0000-0000-000000000001"), "Phone", null, 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000003"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000001"), null, null, "Simulated email", true, false, false, new Guid("00000000-0000-0000-0000-000000000001"), "Email", null, 3, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000004"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000001"), null, null, "Radio communication", true, false, false, new Guid("00000000-0000-0000-0000-000000000001"), "Radio", null, 4, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000005"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000001"), null, null, "Paper document", true, false, false, new Guid("00000000-0000-0000-0000-000000000001"), "Written", null, 5, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000006"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000001"), null, null, "CAX/simulation input", true, false, false, new Guid("00000000-0000-0000-0000-000000000001"), "Simulation", null, 6, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000007"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000001"), null, null, "Custom delivery method (specify in notes)", true, false, true, new Guid("00000000-0000-0000-0000-000000000001"), "Other", null, 7, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Injects_DeliveryMethodId",
                table: "Injects",
                column: "DeliveryMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_Injects_Track",
                table: "Injects",
                column: "Track");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryMethods_OrganizationId_Name",
                table: "DeliveryMethods",
                columns: new[] { "OrganizationId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryMethods_OrganizationId_SortOrder",
                table: "DeliveryMethods",
                columns: new[] { "OrganizationId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpectedOutcomes_InjectId_SortOrder",
                table: "ExpectedOutcomes",
                columns: new[] { "InjectId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_Injects_DeliveryMethods_DeliveryMethodId",
                table: "Injects",
                column: "DeliveryMethodId",
                principalTable: "DeliveryMethods",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Injects_DeliveryMethods_DeliveryMethodId",
                table: "Injects");

            migrationBuilder.DropTable(
                name: "DeliveryMethods");

            migrationBuilder.DropTable(
                name: "ExpectedOutcomes");

            migrationBuilder.DropIndex(
                name: "IX_Injects_DeliveryMethodId",
                table: "Injects");

            migrationBuilder.DropIndex(
                name: "IX_Injects_Track",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "DeliveryMethodId",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "DeliveryMethodOther",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "LocationName",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "LocationType",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "ResponsibleController",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "SourceReference",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "Track",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "TriggerType",
                table: "Injects");
        }
    }
}
