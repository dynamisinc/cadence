using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class RefactorCoreCapabilityToCapability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseTargetCapabilities_CoreCapabilities_CoreCapabilityId",
                table: "ExerciseTargetCapabilities");

            migrationBuilder.DropForeignKey(
                name: "FK_ObservationCapabilities_CoreCapabilities_CoreCapabilityId",
                table: "ObservationCapabilities");

            migrationBuilder.DropTable(
                name: "CoreCapabilities");

            migrationBuilder.RenameColumn(
                name: "CoreCapabilityId",
                table: "ObservationCapabilities",
                newName: "CapabilityId");

            migrationBuilder.RenameIndex(
                name: "IX_ObservationCapabilities_CoreCapabilityId",
                table: "ObservationCapabilities",
                newName: "IX_ObservationCapabilities_CapabilityId");

            migrationBuilder.RenameColumn(
                name: "CoreCapabilityId",
                table: "ExerciseTargetCapabilities",
                newName: "CapabilityId");

            migrationBuilder.RenameIndex(
                name: "IX_ExerciseTargetCapabilities_CoreCapabilityId",
                table: "ExerciseTargetCapabilities",
                newName: "IX_ExerciseTargetCapabilities_CapabilityId");

            migrationBuilder.CreateTable(
                name: "Capabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SourceLibrary = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Capabilities_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Capabilities_Category",
                table: "Capabilities",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Capabilities_IsActive",
                table: "Capabilities",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Capabilities_OrganizationId_Name",
                table: "Capabilities",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseTargetCapabilities_Capabilities_CapabilityId",
                table: "ExerciseTargetCapabilities",
                column: "CapabilityId",
                principalTable: "Capabilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ObservationCapabilities_Capabilities_CapabilityId",
                table: "ObservationCapabilities",
                column: "CapabilityId",
                principalTable: "Capabilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseTargetCapabilities_Capabilities_CapabilityId",
                table: "ExerciseTargetCapabilities");

            migrationBuilder.DropForeignKey(
                name: "FK_ObservationCapabilities_Capabilities_CapabilityId",
                table: "ObservationCapabilities");

            migrationBuilder.DropTable(
                name: "Capabilities");

            migrationBuilder.RenameColumn(
                name: "CapabilityId",
                table: "ObservationCapabilities",
                newName: "CoreCapabilityId");

            migrationBuilder.RenameIndex(
                name: "IX_ObservationCapabilities_CapabilityId",
                table: "ObservationCapabilities",
                newName: "IX_ObservationCapabilities_CoreCapabilityId");

            migrationBuilder.RenameColumn(
                name: "CapabilityId",
                table: "ExerciseTargetCapabilities",
                newName: "CoreCapabilityId");

            migrationBuilder.RenameIndex(
                name: "IX_ExerciseTargetCapabilities_CapabilityId",
                table: "ExerciseTargetCapabilities",
                newName: "IX_ExerciseTargetCapabilities_CoreCapabilityId");

            migrationBuilder.CreateTable(
                name: "CoreCapabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MissionArea = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoreCapabilities", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CoreCapabilities",
                columns: new[] { "Id", "DisplayOrder", "IsActive", "MissionArea", "Name" },
                values: new object[,]
                {
                    { new Guid("00000001-0000-0000-0000-000000000001"), 1, true, "Response", "Planning" },
                    { new Guid("00000001-0000-0000-0000-000000000002"), 2, true, "Response", "Public Information and Warning" },
                    { new Guid("00000001-0000-0000-0000-000000000003"), 3, true, "Response", "Operational Coordination" },
                    { new Guid("00000001-0000-0000-0000-000000000101"), 1, true, "Prevention", "Intelligence and Information Sharing" },
                    { new Guid("00000001-0000-0000-0000-000000000102"), 2, true, "Prevention", "Interdiction and Disruption" },
                    { new Guid("00000001-0000-0000-0000-000000000103"), 3, true, "Prevention", "Screening, Search, and Detection" },
                    { new Guid("00000001-0000-0000-0000-000000000201"), 1, true, "Protection", "Access Control and Identity Verification" },
                    { new Guid("00000001-0000-0000-0000-000000000202"), 2, true, "Protection", "Cybersecurity" },
                    { new Guid("00000001-0000-0000-0000-000000000203"), 3, true, "Protection", "Physical Protective Measures" },
                    { new Guid("00000001-0000-0000-0000-000000000204"), 4, true, "Protection", "Risk Management for Protection Programs and Activities" },
                    { new Guid("00000001-0000-0000-0000-000000000205"), 5, true, "Protection", "Supply Chain Integrity and Security" },
                    { new Guid("00000001-0000-0000-0000-000000000301"), 1, true, "Mitigation", "Community Resilience" },
                    { new Guid("00000001-0000-0000-0000-000000000302"), 2, true, "Mitigation", "Long-term Vulnerability Reduction" },
                    { new Guid("00000001-0000-0000-0000-000000000303"), 3, true, "Mitigation", "Risk and Disaster Resilience Assessment" },
                    { new Guid("00000001-0000-0000-0000-000000000304"), 4, true, "Mitigation", "Threats and Hazard Identification" },
                    { new Guid("00000001-0000-0000-0000-000000000401"), 4, true, "Response", "Critical Transportation" },
                    { new Guid("00000001-0000-0000-0000-000000000402"), 5, true, "Response", "Environmental Response/Health and Safety" },
                    { new Guid("00000001-0000-0000-0000-000000000403"), 6, true, "Response", "Fatality Management Services" },
                    { new Guid("00000001-0000-0000-0000-000000000404"), 7, true, "Response", "Fire Management and Suppression" },
                    { new Guid("00000001-0000-0000-0000-000000000405"), 8, true, "Response", "Infrastructure Systems" },
                    { new Guid("00000001-0000-0000-0000-000000000406"), 9, true, "Response", "Logistics and Supply Chain Management" },
                    { new Guid("00000001-0000-0000-0000-000000000407"), 10, true, "Response", "Mass Care Services" },
                    { new Guid("00000001-0000-0000-0000-000000000408"), 11, true, "Response", "Mass Search and Rescue Operations" },
                    { new Guid("00000001-0000-0000-0000-000000000409"), 12, true, "Response", "On-scene Security, Protection, and Law Enforcement" },
                    { new Guid("00000001-0000-0000-0000-000000000410"), 13, true, "Response", "Operational Communications" },
                    { new Guid("00000001-0000-0000-0000-000000000411"), 14, true, "Response", "Public Health, Healthcare, and Emergency Medical Services" },
                    { new Guid("00000001-0000-0000-0000-000000000412"), 15, true, "Response", "Situational Assessment" },
                    { new Guid("00000001-0000-0000-0000-000000000501"), 1, true, "Recovery", "Economic Recovery" },
                    { new Guid("00000001-0000-0000-0000-000000000502"), 2, true, "Recovery", "Health and Social Services" },
                    { new Guid("00000001-0000-0000-0000-000000000503"), 3, true, "Recovery", "Housing" },
                    { new Guid("00000001-0000-0000-0000-000000000504"), 4, true, "Recovery", "Natural and Cultural Resources" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoreCapabilities_IsActive",
                table: "CoreCapabilities",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CoreCapabilities_MissionArea",
                table: "CoreCapabilities",
                column: "MissionArea");

            migrationBuilder.CreateIndex(
                name: "IX_CoreCapabilities_Name",
                table: "CoreCapabilities",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseTargetCapabilities_CoreCapabilities_CoreCapabilityId",
                table: "ExerciseTargetCapabilities",
                column: "CoreCapabilityId",
                principalTable: "CoreCapabilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ObservationCapabilities_CoreCapabilities_CoreCapabilityId",
                table: "ObservationCapabilities",
                column: "CoreCapabilityId",
                principalTable: "CoreCapabilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
