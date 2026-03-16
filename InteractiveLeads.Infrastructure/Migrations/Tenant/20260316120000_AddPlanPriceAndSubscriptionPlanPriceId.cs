using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddPlanPriceAndSubscriptionPlanPriceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanPrices",
                schema: "Multitenancy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BillingInterval = table.Column<int>(type: "integer", nullable: false),
                    IntervalCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanPrices_Plans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "Multitenancy",
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanPrices_PlanId_BillingInterval_IntervalCount",
                schema: "Multitenancy",
                table: "PlanPrices",
                columns: new[] { "PlanId", "BillingInterval", "IntervalCount" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanPrices_PlanId",
                schema: "Multitenancy",
                table: "PlanPrices",
                column: "PlanId");

            migrationBuilder.AddColumn<Guid>(
                name: "PlanPriceId",
                schema: "Multitenancy",
                table: "Subscriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanPriceId",
                schema: "Multitenancy",
                table: "Subscriptions",
                column: "PlanPriceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_PlanPrices_PlanPriceId",
                schema: "Multitenancy",
                table: "Subscriptions",
                column: "PlanPriceId",
                principalSchema: "Multitenancy",
                principalTable: "PlanPrices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_PlanPrices_PlanPriceId",
                schema: "Multitenancy",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_PlanPriceId",
                schema: "Multitenancy",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PlanPriceId",
                schema: "Multitenancy",
                table: "Subscriptions");

            migrationBuilder.DropTable(
                name: "PlanPrices",
                schema: "Multitenancy");
        }
    }
}
