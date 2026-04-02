using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddSlaPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlaPolicy",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FirstResponseTargetMinutes = table.Column<int>(type: "integer", nullable: false),
                    ResolutionTargetMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaPolicy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlaPolicy_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "Crm",
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlaPolicy_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "Crm",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Team_SlaPolicyId",
                schema: "Crm",
                table: "Team",
                column: "SlaPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Inbox_DefaultSlaPolicyId",
                schema: "Crm",
                table: "Inbox",
                column: "DefaultSlaPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaPolicy_CompanyId",
                schema: "Crm",
                table: "SlaPolicy",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaPolicy_CompanyId_Code",
                schema: "Crm",
                table: "SlaPolicy",
                columns: new[] { "CompanyId", "Code" },
                unique: true,
                filter: "\"Code\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SlaPolicy_CompanyId_CreatedAt",
                schema: "Crm",
                table: "SlaPolicy",
                columns: new[] { "CompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SlaPolicy_CompanyId_IsActive",
                schema: "Crm",
                table: "SlaPolicy",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SlaPolicy_CompanyId_UpdatedAt",
                schema: "Crm",
                table: "SlaPolicy",
                columns: new[] { "CompanyId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SlaPolicy_TenantId",
                schema: "Crm",
                table: "SlaPolicy",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inbox_SlaPolicy_DefaultSlaPolicyId",
                schema: "Crm",
                table: "Inbox",
                column: "DefaultSlaPolicyId",
                principalSchema: "Crm",
                principalTable: "SlaPolicy",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Team_SlaPolicy_SlaPolicyId",
                schema: "Crm",
                table: "Team",
                column: "SlaPolicyId",
                principalSchema: "Crm",
                principalTable: "SlaPolicy",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inbox_SlaPolicy_DefaultSlaPolicyId",
                schema: "Crm",
                table: "Inbox");

            migrationBuilder.DropForeignKey(
                name: "FK_Team_SlaPolicy_SlaPolicyId",
                schema: "Crm",
                table: "Team");

            migrationBuilder.DropTable(
                name: "SlaPolicy",
                schema: "Crm");

            migrationBuilder.DropIndex(
                name: "IX_Team_SlaPolicyId",
                schema: "Crm",
                table: "Team");

            migrationBuilder.DropIndex(
                name: "IX_Inbox_DefaultSlaPolicyId",
                schema: "Crm",
                table: "Inbox");
        }
    }
}
