using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddGitHubIntegrationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GitHubLabelsEnabled",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GitHubOwner",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitHubRepo",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitHubToken",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GitHubIssueNumber",
                table: "FeedbackReports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitHubIssueUrl",
                table: "FeedbackReports",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitHubLabelsEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "GitHubOwner",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "GitHubRepo",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "GitHubToken",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "GitHubIssueNumber",
                table: "FeedbackReports");

            migrationBuilder.DropColumn(
                name: "GitHubIssueUrl",
                table: "FeedbackReports");
        }
    }
}
