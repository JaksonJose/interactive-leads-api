using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddWhatsAppTemplateSubmissionError : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubmissionLastError",
                schema: "Crm",
                table: "WhatsAppTemplate",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmissionLastErrorCode",
                schema: "Crm",
                table: "WhatsAppTemplate",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmissionLastErrorAt",
                schema: "Crm",
                table: "WhatsAppTemplate",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmissionLastError",
                schema: "Crm",
                table: "WhatsAppTemplate");

            migrationBuilder.DropColumn(
                name: "SubmissionLastErrorCode",
                schema: "Crm",
                table: "WhatsAppTemplate");

            migrationBuilder.DropColumn(
                name: "SubmissionLastErrorAt",
                schema: "Crm",
                table: "WhatsAppTemplate");
        }
    }
}
