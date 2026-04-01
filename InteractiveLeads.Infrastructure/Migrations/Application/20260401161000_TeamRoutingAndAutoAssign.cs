using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations.Application;

/// <inheritdoc />
public partial class TeamRoutingAndAutoAssign : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "AutoAssignEnabled",
            schema: "Crm",
            table: "Team",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "AutoAssignIgnoreOfflineUsers",
            schema: "Crm",
            table: "Team",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "AutoAssignMaxConversationsPerUser",
            schema: "Crm",
            table: "Team",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "AutoAssignReassignTimeoutMinutes",
            schema: "Crm",
            table: "Team",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "AutoAssignStrategy",
            schema: "Crm",
            table: "Team",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<Guid>(
            name: "DefaultCalendarId",
            schema: "Crm",
            table: "Inbox",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "DefaultSlaPolicyId",
            schema: "Crm",
            table: "Inbox",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "HandlingTeamId",
            schema: "Crm",
            table: "Conversation",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Priority",
            schema: "Crm",
            table: "InboxTeam",
            type: "integer",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE "Crm"."InboxTeam" AS it
            SET "Priority" = sub.rn
            FROM (
                SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "InboxId" ORDER BY "TeamId") AS rn
                FROM "Crm"."InboxTeam"
            ) AS sub
            WHERE it."Id" = sub."Id";
            """);

        migrationBuilder.AlterColumn<int>(
            name: "Priority",
            schema: "Crm",
            table: "InboxTeam",
            type: "integer",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Conversation_HandlingTeamId",
            schema: "Crm",
            table: "Conversation",
            column: "HandlingTeamId");

        migrationBuilder.CreateIndex(
            name: "IX_Conversation_InboxId_Status_AssignedAgentId",
            schema: "Crm",
            table: "Conversation",
            columns: new[] { "InboxId", "Status", "AssignedAgentId" });

        migrationBuilder.CreateIndex(
            name: "UX_InboxTeam_InboxId_Priority",
            schema: "Crm",
            table: "InboxTeam",
            columns: new[] { "InboxId", "Priority" },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_Conversation_Team_HandlingTeamId",
            schema: "Crm",
            table: "Conversation",
            column: "HandlingTeamId",
            principalSchema: "Crm",
            principalTable: "Team",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Conversation_Team_HandlingTeamId",
            schema: "Crm",
            table: "Conversation");

        migrationBuilder.DropIndex(
            name: "UX_InboxTeam_InboxId_Priority",
            schema: "Crm",
            table: "InboxTeam");

        migrationBuilder.DropIndex(
            name: "IX_Conversation_InboxId_Status_AssignedAgentId",
            schema: "Crm",
            table: "Conversation");

        migrationBuilder.DropIndex(
            name: "IX_Conversation_HandlingTeamId",
            schema: "Crm",
            table: "Conversation");

        migrationBuilder.DropColumn(
            name: "Priority",
            schema: "Crm",
            table: "InboxTeam");

        migrationBuilder.DropColumn(
            name: "HandlingTeamId",
            schema: "Crm",
            table: "Conversation");

        migrationBuilder.DropColumn(
            name: "DefaultSlaPolicyId",
            schema: "Crm",
            table: "Inbox");

        migrationBuilder.DropColumn(
            name: "DefaultCalendarId",
            schema: "Crm",
            table: "Inbox");

        migrationBuilder.DropColumn(
            name: "AutoAssignStrategy",
            schema: "Crm",
            table: "Team");

        migrationBuilder.DropColumn(
            name: "AutoAssignReassignTimeoutMinutes",
            schema: "Crm",
            table: "Team");

        migrationBuilder.DropColumn(
            name: "AutoAssignMaxConversationsPerUser",
            schema: "Crm",
            table: "Team");

        migrationBuilder.DropColumn(
            name: "AutoAssignIgnoreOfflineUsers",
            schema: "Crm",
            table: "Team");

        migrationBuilder.DropColumn(
            name: "AutoAssignEnabled",
            schema: "Crm",
            table: "Team");
    }
}
