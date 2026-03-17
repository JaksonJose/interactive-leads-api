using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationParticipant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConversationParticipant",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationParticipant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationParticipant_Contact_ContactId",
                        column: x => x.ContactId,
                        principalSchema: "Crm",
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConversationParticipant_Conversation_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "Crm",
                        principalTable: "Conversation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_ContactId",
                schema: "Crm",
                table: "ConversationParticipant",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_ConversationId",
                schema: "Crm",
                table: "ConversationParticipant",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_ConversationId_ContactId_Active",
                schema: "Crm",
                table: "ConversationParticipant",
                columns: new[] { "ConversationId", "ContactId" },
                unique: true,
                filter: "\"IsActive\" = true AND \"ContactId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_ConversationId_UserId_Active",
                schema: "Crm",
                table: "ConversationParticipant",
                columns: new[] { "ConversationId", "UserId" },
                unique: true,
                filter: "\"IsActive\" = true AND \"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_IsActive",
                schema: "Crm",
                table: "ConversationParticipant",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_UserId",
                schema: "Crm",
                table: "ConversationParticipant",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationParticipant",
                schema: "Crm");
        }
    }
}
