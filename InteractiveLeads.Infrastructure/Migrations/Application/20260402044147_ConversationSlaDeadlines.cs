using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class ConversationSlaDeadlines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EffectiveSlaPolicyId",
                schema: "Crm",
                table: "Conversation",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FirstAgentResponseAt",
                schema: "Crm",
                table: "Conversation",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FirstResponseDueAt",
                schema: "Crm",
                table: "Conversation",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResolutionDueAt",
                schema: "Crm",
                table: "Conversation",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_EffectiveSlaPolicyId",
                schema: "Crm",
                table: "Conversation",
                column: "EffectiveSlaPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_FirstResponseDueAt",
                schema: "Crm",
                table: "Conversation",
                column: "FirstResponseDueAt");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversation_SlaPolicy_EffectiveSlaPolicyId",
                schema: "Crm",
                table: "Conversation",
                column: "EffectiveSlaPolicyId",
                principalSchema: "Crm",
                principalTable: "SlaPolicy",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversation_SlaPolicy_EffectiveSlaPolicyId",
                schema: "Crm",
                table: "Conversation");

            migrationBuilder.DropIndex(
                name: "IX_Conversation_EffectiveSlaPolicyId",
                schema: "Crm",
                table: "Conversation");

            migrationBuilder.DropIndex(
                name: "IX_Conversation_FirstResponseDueAt",
                schema: "Crm",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "EffectiveSlaPolicyId",
                schema: "Crm",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "FirstAgentResponseAt",
                schema: "Crm",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "FirstResponseDueAt",
                schema: "Crm",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "ResolutionDueAt",
                schema: "Crm",
                table: "Conversation");
        }
    }
}
