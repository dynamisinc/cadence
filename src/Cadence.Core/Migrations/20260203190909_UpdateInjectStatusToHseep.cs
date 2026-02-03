using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <summary>
    /// Migration to update InjectStatus values from legacy to HSEEP-compliant values.
    ///
    /// Old values:
    ///   Pending = 0, Ready = 1, Fired = 2, Skipped = 3
    ///
    /// New HSEEP values:
    ///   Draft = 0, Submitted = 1, Approved = 2, Synchronized = 3,
    ///   Released = 4, Complete = 5, Deferred = 6, Obsolete = 7
    ///
    /// Migration mapping:
    ///   Pending (0) → Draft (0)         [no change - same value]
    ///   Ready (1)   → Synchronized (3)  [needs migration]
    ///   Fired (2)   → Released (4)      [needs migration]
    ///   Skipped (3) → Deferred (6)      [needs migration]
    /// </summary>
    public partial class UpdateInjectStatusToHseep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IMPORTANT: Order matters! Process in reverse value order to avoid collisions.
            // We must not overwrite values that haven't been migrated yet.

            // Step 1: Skipped (3) → Deferred (6)
            // Must be done first since 3 will be used for Synchronized
            migrationBuilder.Sql("UPDATE Injects SET Status = 6 WHERE Status = 3");

            // Step 2: Fired (2) → Released (4)
            migrationBuilder.Sql("UPDATE Injects SET Status = 4 WHERE Status = 2");

            // Step 3: Ready (1) → Synchronized (3)
            // Now safe since 3 has been migrated to 6
            migrationBuilder.Sql("UPDATE Injects SET Status = 3 WHERE Status = 1");

            // Note: Pending (0) → Draft (0) requires no change
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the migration - process in forward order

            // Step 1: Synchronized (3) → Ready (1)
            migrationBuilder.Sql("UPDATE Injects SET Status = 1 WHERE Status = 3");

            // Step 2: Released (4) → Fired (2)
            migrationBuilder.Sql("UPDATE Injects SET Status = 2 WHERE Status = 4");

            // Step 3: Deferred (6) → Skipped (3)
            migrationBuilder.Sql("UPDATE Injects SET Status = 3 WHERE Status = 6");

            // Note: Any new HSEEP-only statuses (Submitted=1, Approved=2, Complete=5, Obsolete=7)
            // would need to be handled. For simplicity, reset them to Draft (0).
            migrationBuilder.Sql("UPDATE Injects SET Status = 0 WHERE Status IN (1, 2, 5, 7)");
        }
    }
}
