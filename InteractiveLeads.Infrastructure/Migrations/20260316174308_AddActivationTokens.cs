using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActivationTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivationTokens",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivationTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivationTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivationTokens_ExpiresAt",
                schema: "Identity",
                table: "ActivationTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActivationTokens_Token",
                schema: "Identity",
                table: "ActivationTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivationTokens_UserId",
                schema: "Identity",
                table: "ActivationTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivationTokens",
                schema: "Identity");
        }
    }
}
