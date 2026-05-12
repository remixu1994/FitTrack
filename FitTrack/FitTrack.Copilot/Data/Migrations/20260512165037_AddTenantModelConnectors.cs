using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTrack.Copilot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantModelConnectors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var seededAt = new DateTime(2026, 5, 13, 0, 0, 0, DateTimeKind.Utc);

            migrationBuilder.AddColumn<string>(
                name: "PreferredModelConnectorId",
                table: "UserProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "tenant-default");

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    IsSystemDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantModelConnectors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    ProviderPreset = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Protocol = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    BaseUrl = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    ModelId = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    EncryptedApiKey = table.Column<string>(type: "TEXT", nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantModelConnectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantModelConnectors_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "Name", "Slug", "IsSystemDefault", "CreatedAt", "UpdatedAt" },
                values: new object[] { "tenant-default", "Default Tenant", "default", true, seededAt, seededAt });

            migrationBuilder.InsertData(
                table: "TenantModelConnectors",
                columns: new[] { "Id", "TenantId", "DisplayName", "ProviderPreset", "Protocol", "BaseUrl", "ModelId", "EncryptedApiKey", "IsDefault", "IsEnabled", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { "connector-default-xiaomi-mimo", "tenant-default", "Xiaomi MiMo", "xiaomi-mimo", "OpenAICompatible", "https://token-plan-cn.xiaomimimo.com/v1", "mimo-v2.5", null, true, true, seededAt, seededAt },
                    { "connector-default-minimax", "tenant-default", "MiniMax", "minimax", "OpenAICompatible", "https://api.minimax.chat/v1", "MiniMax-Text-01", null, false, true, seededAt, seededAt },
                    { "connector-default-openai-codex", "tenant-default", "OpenAI Codex", "openai-codex", "OpenAICompatible", "https://api.openai.com/v1", "codex-mini-latest", null, false, true, seededAt, seededAt },
                    { "connector-default-qwen", "tenant-default", "Qwen", "qwen", "OpenAICompatible", "https://dashscope.aliyuncs.com/compatible-mode/v1", "qwen-plus", null, false, true, seededAt, seededAt },
                    { "connector-default-glm", "tenant-default", "GLM", "glm", "OpenAICompatible", "https://open.bigmodel.cn/api/paas/v4", "glm-4.5", null, false, true, seededAt, seededAt },
                    { "connector-default-openai-compatible", "tenant-default", "OpenAI Compatible", "openai-compatible", "OpenAICompatible", "https://api.openai.com/v1", "gpt-4.1-mini", null, false, true, seededAt, seededAt },
                    { "connector-default-azure-openai", "tenant-default", "Azure OpenAI", "azure-openai", "AzureOpenAI", "https://your-resource.openai.azure.com", "gpt-4o", null, false, true, seededAt, seededAt },
                    { "connector-default-anthropic", "tenant-default", "Anthropic", "anthropic", "Anthropic", "https://api.anthropic.com", "claude-sonnet-4-20250514", null, false, true, seededAt, seededAt }
                });

            migrationBuilder.Sql("""
                UPDATE AspNetUsers
                SET TenantId = 'tenant-default'
                WHERE TenantId IS NULL OR TenantId = '';
                """);

            migrationBuilder.Sql("""
                UPDATE UserProfiles
                SET PreferredModelConnectorId =
                    CASE PreferredAIProvider
                        WHEN 'AzureOpenAI' THEN 'connector-default-azure-openai'
                        WHEN 'MiniMax' THEN 'connector-default-minimax'
                        WHEN 'Xiaomi' THEN 'connector-default-xiaomi-mimo'
                        ELSE NULL
                    END
                WHERE PreferredAIProvider IS NOT NULL AND TRIM(PreferredAIProvider) <> '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_PreferredModelConnectorId",
                table: "UserProfiles",
                column: "PreferredModelConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TenantId",
                table: "AspNetUsers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantModelConnectors_TenantId_DisplayName",
                table: "TenantModelConnectors",
                columns: new[] { "TenantId", "DisplayName" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantModelConnectors_TenantId_IsDefault",
                table: "TenantModelConnectors",
                columns: new[] { "TenantId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Tenants_TenantId",
                table: "AspNetUsers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "PreferredAIProvider",
                table: "UserProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredAIProvider",
                table: "UserProfiles",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE UserProfiles
                SET PreferredAIProvider =
                    CASE PreferredModelConnectorId
                        WHEN 'connector-default-azure-openai' THEN 'AzureOpenAI'
                        WHEN 'connector-default-minimax' THEN 'MiniMax'
                        WHEN 'connector-default-xiaomi-mimo' THEN 'Xiaomi'
                        ELSE NULL
                    END;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Tenants_TenantId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "TenantModelConnectors");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_PreferredModelConnectorId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TenantId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PreferredModelConnectorId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AspNetUsers");
        }
    }
}
