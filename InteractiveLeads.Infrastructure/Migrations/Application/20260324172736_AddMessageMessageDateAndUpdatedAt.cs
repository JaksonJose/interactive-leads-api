using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddMessageMessageDateAndUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve former "message instant" from legacy CreatedAt, then redefine CreatedAt as system insert time.
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MessageDate",
                schema: "Crm",
                table: "Message",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "Crm",
                table: "Message",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Crm"."Message" SET "MessageDate" = "CreatedAt" WHERE "MessageDate" IS NULL;
                UPDATE "Crm"."Message" SET "UpdatedAt" = "MessageDate" WHERE "UpdatedAt" IS NULL;
                UPDATE "Crm"."Message" SET "CreatedAt" = NOW() AT TIME ZONE 'utc';
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "MessageDate",
                schema: "Crm",
                table: "Message",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "Crm",
                table: "Message",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now() at time zone 'utc'",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Message_ConversationId_MessageDate",
                schema: "Crm",
                table: "Message",
                columns: new[] { "ConversationId", "MessageDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Message_ConversationId_MessageDate",
                schema: "Crm",
                table: "Message");

            migrationBuilder.DropColumn(
                name: "MessageDate",
                schema: "Crm",
                table: "Message");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "Crm",
                table: "Message");
        }
    }
}
