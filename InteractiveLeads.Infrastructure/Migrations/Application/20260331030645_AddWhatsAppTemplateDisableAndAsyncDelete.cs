using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddWhatsAppTemplateDisableAndAsyncDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeleteLastError",
                schema: "Crm",
                table: "WhatsAppTemplate",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeleteLastErrorAt",
                schema: "Crm",
                table: "WhatsAppTemplate",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeleteLastErrorCode",
                schema: "Crm",
                table: "WhatsAppTemplate",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DeletePending",
                schema: "Crm",
                table: "WhatsAppTemplate",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeleteRequestedAt",
                schema: "Crm",
                table: "WhatsAppTemplate",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DisabledAt",
                schema: "Crm",
                table: "WhatsAppTemplate",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisabledReason",
                schema: "Crm",
                table: "WhatsAppTemplate",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDisabled",
                schema: "Crm",
                table: "WhatsAppTemplate",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeleteLastError",
                schema: "Crm",
                table: "WhatsAppTemplate");

            migrationBuilder.DropColumn(
                name: "DeleteLastErrorAt",
                schema: "Crm",
                table: "WhatsAppTemplate");

            migrationBuilder.DropColumn(
                name: "DeleteLastErrorCode",
                schema: "Crm",
                table: "WhatsAppTemplate");

            migrationBuilder.DropColumn(
                name: "DeletePending",
                schema: "Crm",
                table: "WhatsAppTemplate");

            migrationBuilder.DropColumn(
                name: "DeleteRequestedAt",
                schema: "Crm",
                table: "WhatsAppTemplate");

            migrationBuilder.DropColumn(
                name: "DisabledAt",
                schema: "Crm",
                table: "WhatsAppTemplate");

            migrationBuilder.DropColumn(
                name: "DisabledReason",
                schema: "Crm",
                table: "WhatsAppTemplate");

            migrationBuilder.DropColumn(
                name: "IsDisabled",
                schema: "Crm",
                table: "WhatsAppTemplate");
        }
    }
}
