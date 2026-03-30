using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddWhatsAppWabaAndTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WhatsAppBusinessAccountId",
                schema: "Crm",
                table: "Integration",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WhatsAppBusinessAccount",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WabaId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppBusinessAccount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppBusinessAccount_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "Crm",
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppTemplate",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetaTemplateId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Language = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WhatsAppBusinessAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentsJson = table.Column<string>(type: "jsonb", nullable: false),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppTemplate_WhatsAppBusinessAccount_WhatsAppBusinessAc~",
                        column: x => x.WhatsAppBusinessAccountId,
                        principalSchema: "Crm",
                        principalTable: "WhatsAppBusinessAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Integration_WhatsAppBusinessAccountId",
                schema: "Crm",
                table: "Integration",
                column: "WhatsAppBusinessAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppBusinessAccount_Company_Waba",
                schema: "Crm",
                table: "WhatsAppBusinessAccount",
                columns: new[] { "CompanyId", "WabaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppBusinessAccount_CompanyId",
                schema: "Crm",
                table: "WhatsAppBusinessAccount",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplate_Waba_Name_Language",
                schema: "Crm",
                table: "WhatsAppTemplate",
                columns: new[] { "WhatsAppBusinessAccountId", "Name", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplate_WhatsAppBusinessAccountId",
                schema: "Crm",
                table: "WhatsAppTemplate",
                column: "WhatsAppBusinessAccountId");

            // Backfill WABA rows and link existing WhatsApp integrations (Settings.businessAccountId → WhatsAppBusinessAccount).
            migrationBuilder.Sql(
                """
                INSERT INTO "Crm"."WhatsAppBusinessAccount" ("Id", "WabaId", "CompanyId", "Name", "CreatedAt")
                SELECT gen_random_uuid(), d."WabaId", d."CompanyId", NULL, now()
                FROM (
                  SELECT DISTINCT i."CompanyId", trim(both ' ' from (i."Settings"->>'businessAccountId')) AS "WabaId"
                  FROM "Crm"."Integration" i
                  WHERE i."Type" = 1
                    AND i."Settings" IS NOT NULL
                    AND nullif(trim(both ' ' from (i."Settings"->>'businessAccountId')), '') IS NOT NULL
                ) d
                WHERE NOT EXISTS (
                  SELECT 1 FROM "Crm"."WhatsAppBusinessAccount" w
                  WHERE w."CompanyId" = d."CompanyId" AND w."WabaId" = d."WabaId"
                );
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Crm"."Integration" i
                SET "WhatsAppBusinessAccountId" = w."Id"
                FROM "Crm"."WhatsAppBusinessAccount" w
                WHERE i."Type" = 1
                  AND i."CompanyId" = w."CompanyId"
                  AND trim(both ' ' from coalesce(i."Settings"->>'businessAccountId', '')) = w."WabaId"
                  AND nullif(trim(both ' ' from coalesce(i."Settings"->>'businessAccountId', '')), '') IS NOT NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Integration_WhatsAppBusinessAccount_WhatsAppBusinessAccount~",
                schema: "Crm",
                table: "Integration",
                column: "WhatsAppBusinessAccountId",
                principalSchema: "Crm",
                principalTable: "WhatsAppBusinessAccount",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Integration_WhatsAppBusinessAccount_WhatsAppBusinessAccount~",
                schema: "Crm",
                table: "Integration");

            migrationBuilder.DropTable(
                name: "WhatsAppTemplate",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "WhatsAppBusinessAccount",
                schema: "Crm");

            migrationBuilder.DropIndex(
                name: "IX_Integration_WhatsAppBusinessAccountId",
                schema: "Crm",
                table: "Integration");

            migrationBuilder.DropColumn(
                name: "WhatsAppBusinessAccountId",
                schema: "Crm",
                table: "Integration");
        }
    }
}
