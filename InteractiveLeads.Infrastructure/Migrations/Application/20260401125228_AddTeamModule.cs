using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddTeamModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Team",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    CalendarId = table.Column<Guid>(type: "uuid", nullable: true),
                    SlaPolicyId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Team", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Team_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "Crm",
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Team_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "Crm",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InboxTeam",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxTeam", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InboxTeam_Inbox_InboxId",
                        column: x => x.InboxId,
                        principalSchema: "Crm",
                        principalTable: "Inbox",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InboxTeam_Team_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "Crm",
                        principalTable: "Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTeam",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: true),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTeam", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTeam_Team_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "Crm",
                        principalTable: "Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboxTeam_InboxId",
                schema: "Crm",
                table: "InboxTeam",
                column: "InboxId");

            migrationBuilder.CreateIndex(
                name: "IX_InboxTeam_TeamId",
                schema: "Crm",
                table: "InboxTeam",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "UX_InboxTeam_InboxId_TeamId",
                schema: "Crm",
                table: "InboxTeam",
                columns: new[] { "InboxId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Team_CompanyId",
                schema: "Crm",
                table: "Team",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Team_TenantId",
                schema: "Crm",
                table: "Team",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTeam_TeamId",
                schema: "Crm",
                table: "UserTeam",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTeam_UserId",
                schema: "Crm",
                table: "UserTeam",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_UserTeam_TeamId_UserId",
                schema: "Crm",
                table: "UserTeam",
                columns: new[] { "TeamId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxTeam",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "UserTeam",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "Team",
                schema: "Crm");
        }
    }
}
