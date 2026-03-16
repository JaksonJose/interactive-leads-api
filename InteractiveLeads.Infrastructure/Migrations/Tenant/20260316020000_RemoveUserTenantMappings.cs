using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class RemoveUserTenantMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserTenantMappings",
                schema: "Multitenancy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserTenantMappings",
                schema: "Multitenancy",
                columns: table => new
                {
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTenantMappings", x => x.Email);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantMappings_Email_IsActive",
                schema: "Multitenancy",
                table: "UserTenantMappings",
                columns: new[] { "Email", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantMappings_Email_Unique",
                schema: "Multitenancy",
                table: "UserTenantMappings",
                column: "Email",
                unique: true);
        }
    }
}
