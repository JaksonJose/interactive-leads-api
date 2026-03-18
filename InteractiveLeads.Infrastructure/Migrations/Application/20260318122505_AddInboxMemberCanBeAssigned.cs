using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddInboxMemberCanBeAssigned : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanBeAssigned",
                schema: "Crm",
                table: "InboxMember",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanBeAssigned",
                schema: "Crm",
                table: "InboxMember");
        }
    }
}
