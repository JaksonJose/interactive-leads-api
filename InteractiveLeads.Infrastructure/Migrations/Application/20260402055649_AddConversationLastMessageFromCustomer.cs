using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddConversationLastMessageFromCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LastMessageFromCustomer",
                schema: "Crm",
                table: "Conversation",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Direction 1 = Inbound (see MessageDirection enum)
            migrationBuilder.Sql(
                """
                UPDATE "Crm"."Conversation" c
                SET "LastMessageFromCustomer" = m.is_inbound
                FROM (
                    SELECT DISTINCT ON ("ConversationId")
                        "ConversationId",
                        ("Direction" = 1) AS is_inbound
                    FROM "Crm"."Message"
                    ORDER BY "ConversationId", "MessageDate" DESC, "Id" DESC
                ) m
                WHERE c."Id" = m."ConversationId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_InactivityReassign",
                schema: "Crm",
                table: "Conversation",
                columns: new[] { "Status", "HandlingTeamId", "LastMessageFromCustomer", "LastMessageAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Conversation_InactivityReassign",
                schema: "Crm",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "LastMessageFromCustomer",
                schema: "Crm",
                table: "Conversation");
        }
    }
}
