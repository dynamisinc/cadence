using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <summary>
    /// Migration to update InjectStatus values from legacy to HSEEP-compliant values.
    ///
    /// Old string values: Pending, Ready, Fired, Skipped
    /// New HSEEP string values: Draft, Submitted, Approved, Synchronized, Released, Complete, Deferred, Obsolete
    ///
    /// Migration mapping:
    ///   Pending  → Draft        (initial status)
    ///   Ready    → Synchronized (scheduled for delivery)
    ///   Fired    → Released     (delivered to players)
    ///   Skipped  → Deferred     (cancelled before delivery)
    /// </summary>
    public partial class UpdateInjectStatusToHseep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Status is stored as nvarchar (string), so use string values
            // Update old terminology to new HSEEP terminology

            // Pending → Draft (initial authoring status)
            migrationBuilder.Sql("UPDATE Injects SET Status = 'Draft' WHERE Status = 'Pending'");

            // Ready → Synchronized (scheduled for a specific time)
            migrationBuilder.Sql("UPDATE Injects SET Status = 'Synchronized' WHERE Status = 'Ready'");

            // Fired → Released (delivered to players)
            migrationBuilder.Sql("UPDATE Injects SET Status = 'Released' WHERE Status = 'Fired'");

            // Skipped → Deferred (cancelled before delivery)
            migrationBuilder.Sql("UPDATE Injects SET Status = 'Deferred' WHERE Status = 'Skipped'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the migration - convert HSEEP terms back to legacy terms

            // Draft → Pending
            migrationBuilder.Sql("UPDATE Injects SET Status = 'Pending' WHERE Status = 'Draft'");

            // Synchronized → Ready
            migrationBuilder.Sql("UPDATE Injects SET Status = 'Ready' WHERE Status = 'Synchronized'");

            // Released → Fired
            migrationBuilder.Sql("UPDATE Injects SET Status = 'Fired' WHERE Status = 'Released'");

            // Deferred → Skipped
            migrationBuilder.Sql("UPDATE Injects SET Status = 'Skipped' WHERE Status = 'Deferred'");

            // Note: Any new HSEEP-only statuses (Submitted, Approved, Complete, Obsolete)
            // would need to be handled. Reset them to Pending for safety.
            migrationBuilder.Sql("UPDATE Injects SET Status = 'Pending' WHERE Status IN ('Submitted', 'Approved', 'Complete', 'Obsolete')");
        }
    }
}
