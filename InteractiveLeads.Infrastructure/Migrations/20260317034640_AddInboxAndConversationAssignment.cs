using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInboxAndConversationAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AssignedAt",
                schema: "Crm",
                table: "Conversation",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InboxId",
                schema: "Crm",
                table: "Conversation",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                schema: "Crm",
                table: "Conversation",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ConversationAssignment",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AssignedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UnassignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationAssignment_Conversation_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "Crm",
                        principalTable: "Conversation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inbox",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inbox_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "Crm",
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InboxMember",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
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
                name: "IX_Conversation_InboxId",
                schema: "Crm",
                table: "Conversation",
                column: "InboxId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_LastMessageAt",
                schema: "Crm",
                table: "Conversation",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_Priority",
                schema: "Crm",
                table: "Conversation",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_Status",
                schema: "Crm",
                table: "Conversation",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationAssignment_AssignedAt",
                schema: "Crm",
                table: "ConversationAssignment",
                column: "AssignedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationAssignment_ConversationId",
                schema: "Crm",
                table: "ConversationAssignment",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationAssignment_ConversationId_UserId_Active",
                schema: "Crm",
                table: "ConversationAssignment",
                columns: new[] { "ConversationId", "UserId" },
                unique: true,
                filter: "\"UnassignedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationAssignment_UserId",
                schema: "Crm",
                table: "ConversationAssignment",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Inbox_CompanyId",
                schema: "Crm",
                table: "Inbox",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Inbox_IsActive",
                schema: "Crm",
                table: "Inbox",
                column: "IsActive");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Conversation_Inbox_InboxId",
                schema: "Crm",
                table: "Conversation",
                column: "InboxId",
                principalSchema: "Crm",
                principalTable: "Inbox",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversation_Inbox_InboxId",
                schema: "Crm",
                table: "Conversation");

            migrationBuilder.DropTable(
                name: "ConversationAssignment",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "InboxMember",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "Inbox",
                schema: "Crm");

            migrationBuilder.DropIndex(
                name: "IX_Conversation_InboxId",
                schema: "Crm",
                table: "Conversation");

            migrationBuilder.DropIndex(
                name: "IX_Conversation_LastMessageAt",
                schema: "Crm",
                table: "Conversation");

            migrationBuilder.DropIndex(
                name: "IX_Conversation_Priority",
                schema: "Crm",
                table: "Conversation");

            migrationBuilder.DropIndex(
                name: "IX_Conversation_Status",
                schema: "Crm",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                schema: "Crm",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "InboxId",
                schema: "Crm",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "Priority",
                schema: "Crm",
                table: "Conversation");
        }
    }
}
