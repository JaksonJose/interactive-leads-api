using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddTeamAutoReassignOnSlaExpired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoReassignOnFirstResponseSlaExpired",
                schema: "Crm",
                table: "Team",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoReassignOnFirstResponseSlaExpired",
                schema: "Crm",
                table: "Team");
        }
    }
}
