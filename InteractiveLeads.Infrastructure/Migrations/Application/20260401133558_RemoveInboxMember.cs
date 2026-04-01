using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class RemoveInboxMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxMember",
                schema: "Crm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InboxMember",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanBeAssigned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    Role = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InboxMember_Inbox_InboxId",
                        column: x => x.InboxId,
                        principalSchema: "Crm",
                        principalTable: "Inbox",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboxMember_InboxId",
                schema: "Crm",
                table: "InboxMember",
                column: "InboxId");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMember_InboxId_UserId_Active",
                schema: "Crm",
                table: "InboxMember",
                columns: new[] { "InboxId", "UserId" },
                unique: true,
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMember_IsActive",
                schema: "Crm",
                table: "InboxMember",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMember_UserId",
                schema: "Crm",
                table: "InboxMember",
                column: "UserId");
        }
    }
}
