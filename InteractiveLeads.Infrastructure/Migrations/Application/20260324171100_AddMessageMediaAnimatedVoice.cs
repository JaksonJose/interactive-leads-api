using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddMessageMediaAnimatedVoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Animated",
                schema: "Crm",
                table: "MessageMedia",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Voice",
                schema: "Crm",
                table: "MessageMedia",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Animated",
                schema: "Crm",
                table: "MessageMedia");

            migrationBuilder.DropColumn(
                name: "Voice",
                schema: "Crm",
                table: "MessageMedia");
        }
    }
}
