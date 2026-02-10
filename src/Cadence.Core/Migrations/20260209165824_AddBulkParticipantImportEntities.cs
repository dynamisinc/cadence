using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddBulkParticipantImportEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BulkImportRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    AssignedCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedCount = table.Column<int>(type: "int", nullable: false),
                    InvitedCount = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkImportRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BulkImportRecords_AspNetUsers_ImportedById",
                        column: x => x.ImportedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BulkImportRecords_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BulkImportRowResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BulkImportRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExerciseRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Classification = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PreviousExerciseRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkImportRowResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BulkImportRowResults_BulkImportRecords_BulkImportRecordId",
                        column: x => x.BulkImportRecordId,
                        principalTable: "BulkImportRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingExerciseAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationInviteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BulkImportRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingExerciseAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingExerciseAssignments_BulkImportRecords_BulkImportRecordId",
                        column: x => x.BulkImportRecordId,
                        principalTable: "BulkImportRecords",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PendingExerciseAssignments_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PendingExerciseAssignments_OrganizationInvites_OrganizationInviteId",
                        column: x => x.OrganizationInviteId,
                        principalTable: "OrganizationInvites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BulkImportRecords_ExerciseId",
                table: "BulkImportRecords",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_BulkImportRecords_ExerciseId_ImportedAt",
                table: "BulkImportRecords",
                columns: new[] { "ExerciseId", "ImportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BulkImportRecords_ImportedById",
                table: "BulkImportRecords",
                column: "ImportedById");

            migrationBuilder.CreateIndex(
                name: "IX_BulkImportRowResults_BulkImportRecordId",
                table: "BulkImportRowResults",
                column: "BulkImportRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_BulkImportRowResults_BulkImportRecordId_Classification",
                table: "BulkImportRowResults",
                columns: new[] { "BulkImportRecordId", "Classification" });

            migrationBuilder.CreateIndex(
                name: "IX_PendingExerciseAssignments_BulkImportRecordId",
                table: "PendingExerciseAssignments",
                column: "BulkImportRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingExerciseAssignments_ExerciseId",
                table: "PendingExerciseAssignments",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingExerciseAssignments_ExerciseId_Status",
                table: "PendingExerciseAssignments",
                columns: new[] { "ExerciseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PendingExerciseAssignments_OrganizationInviteId",
                table: "PendingExerciseAssignments",
                column: "OrganizationInviteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BulkImportRowResults");

            migrationBuilder.DropTable(
                name: "PendingExerciseAssignments");

            migrationBuilder.DropTable(
                name: "BulkImportRecords");
        }
    }
}
