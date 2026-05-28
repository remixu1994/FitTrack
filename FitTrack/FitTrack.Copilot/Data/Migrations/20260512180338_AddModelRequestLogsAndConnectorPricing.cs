using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTrack.Copilot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModelRequestLogsAndConnectorPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CacheReadTokenPricePer1M",
                table: "TenantModelConnectors",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CacheWriteTokenPricePer1M",
                table: "TenantModelConnectors",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "InputTokenPricePer1M",
                table: "TenantModelConnectors",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OutputTokenPricePer1M",
                table: "TenantModelConnectors",
                type: "REAL",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ModelRequestLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ThreadId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ConversationMessageId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ConnectorId = table.Column<string>(type: "TEXT", nullable: false),
                    ConnectorDisplayName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    ProviderPreset = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Protocol = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ModelId = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    RequestType = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DurationMs = table.Column<int>(type: "INTEGER", nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    ClientIpHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    InputTokens = table.Column<long>(type: "INTEGER", nullable: true),
                    OutputTokens = table.Column<long>(type: "INTEGER", nullable: true),
                    TotalTokens = table.Column<long>(type: "INTEGER", nullable: true),
                    CacheReadTokens = table.Column<long>(type: "INTEGER", nullable: true),
                    CacheWriteTokens = table.Column<long>(type: "INTEGER", nullable: true),
                    InputCostUsd = table.Column<double>(type: "REAL", nullable: true),
                    OutputCostUsd = table.Column<double>(type: "REAL", nullable: true),
                    CacheReadCostUsd = table.Column<double>(type: "REAL", nullable: true),
                    CacheWriteCostUsd = table.Column<double>(type: "REAL", nullable: true),
                    TotalCostUsd = table.Column<double>(type: "REAL", nullable: true),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    ToolEventsSummary = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    RequestSummary = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelRequestLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModelRequestLogs_TenantId_ConnectorId_StartedAtUtc",
                table: "ModelRequestLogs",
                columns: new[] { "TenantId", "ConnectorId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelRequestLogs_TenantId_StartedAtUtc",
                table: "ModelRequestLogs",
                columns: new[] { "TenantId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelRequestLogs_TenantId_Status_StartedAtUtc",
                table: "ModelRequestLogs",
                columns: new[] { "TenantId", "Status", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModelRequestLogs");

            migrationBuilder.DropColumn(
                name: "CacheReadTokenPricePer1M",
                table: "TenantModelConnectors");

            migrationBuilder.DropColumn(
                name: "CacheWriteTokenPricePer1M",
                table: "TenantModelConnectors");

            migrationBuilder.DropColumn(
                name: "InputTokenPricePer1M",
                table: "TenantModelConnectors");

            migrationBuilder.DropColumn(
                name: "OutputTokenPricePer1M",
                table: "TenantModelConnectors");
        }
    }
}
