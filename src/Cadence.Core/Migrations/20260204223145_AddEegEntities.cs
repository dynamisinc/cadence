using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddEegEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CapabilityTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CapabilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_CapabilityTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapabilityTargets_Capabilities_CapabilityId",
                        column: x => x.CapabilityId,
                        principalTable: "Capabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CapabilityTargets_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CapabilityTargets_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CriticalTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Standard = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CapabilityTargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_CriticalTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CriticalTasks_CapabilityTargets_CapabilityTargetId",
                        column: x => x.CapabilityTargetId,
                        principalTable: "CapabilityTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EegEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObservationText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Rating = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ObservedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriticalTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluatorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TriggeringInjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_EegEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EegEntries_AspNetUsers_EvaluatorId",
                        column: x => x.EvaluatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EegEntries_CriticalTasks_CriticalTaskId",
                        column: x => x.CriticalTaskId,
                        principalTable: "CriticalTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EegEntries_Injects_TriggeringInjectId",
                        column: x => x.TriggeringInjectId,
                        principalTable: "Injects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EegEntries_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InjectCriticalTasks",
                columns: table => new
                {
                    InjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriticalTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InjectCriticalTasks", x => new { x.InjectId, x.CriticalTaskId });
                    table.ForeignKey(
                        name: "FK_InjectCriticalTasks_CriticalTasks_CriticalTaskId",
                        column: x => x.CriticalTaskId,
                        principalTable: "CriticalTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InjectCriticalTasks_Injects_InjectId",
                        column: x => x.InjectId,
                        principalTable: "Injects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CapabilityTargets_CapabilityId",
                table: "CapabilityTargets",
                column: "CapabilityId");

            migrationBuilder.CreateIndex(
                name: "IX_CapabilityTargets_ExerciseId",
                table: "CapabilityTargets",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_CapabilityTargets_ExerciseId_SortOrder",
                table: "CapabilityTargets",
                columns: new[] { "ExerciseId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CapabilityTargets_OrganizationId",
                table: "CapabilityTargets",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CriticalTasks_CapabilityTargetId",
                table: "CriticalTasks",
                column: "CapabilityTargetId");

            migrationBuilder.CreateIndex(
                name: "IX_CriticalTasks_CapabilityTargetId_SortOrder",
                table: "CriticalTasks",
                columns: new[] { "CapabilityTargetId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_EegEntries_CriticalTaskId",
                table: "EegEntries",
                column: "CriticalTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_EegEntries_CriticalTaskId_ObservedAt",
                table: "EegEntries",
                columns: new[] { "CriticalTaskId", "ObservedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EegEntries_EvaluatorId",
                table: "EegEntries",
                column: "EvaluatorId");

            migrationBuilder.CreateIndex(
                name: "IX_EegEntries_ObservedAt",
                table: "EegEntries",
                column: "ObservedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EegEntries_OrganizationId",
                table: "EegEntries",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_EegEntries_TriggeringInjectId",
                table: "EegEntries",
                column: "TriggeringInjectId");

            migrationBuilder.CreateIndex(
                name: "IX_InjectCriticalTasks_CriticalTaskId",
                table: "InjectCriticalTasks",
                column: "CriticalTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_InjectCriticalTasks_InjectId",
                table: "InjectCriticalTasks",
                column: "InjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EegEntries");

            migrationBuilder.DropTable(
                name: "InjectCriticalTasks");

            migrationBuilder.DropTable(
                name: "CriticalTasks");

            migrationBuilder.DropTable(
                name: "CapabilityTargets");
        }
    }
}
