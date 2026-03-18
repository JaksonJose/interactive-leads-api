using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Multitenancy");

            migrationBuilder.CreateTable(
                name: "ActivationTokenLookups",
                schema: "Multitenancy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Used = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivationTokenLookups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationLookups",
                schema: "Multitenancy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IntegrationType = table.Column<int>(type: "integer", nullable: false),
                    ExternalIdentifier = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationLookups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                schema: "Multitenancy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Identifier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "Multitenancy",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConnectionString = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanFeatures",
                schema: "Multitenancy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanFeatures_Plans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "Multitenancy",
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanLimits",
                schema: "Multitenancy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    LimitKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LimitValue = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanLimits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanLimits_Plans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "Multitenancy",
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                schema: "Multitenancy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanPriceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_PlanPrices_PlanPriceId",
                        column: x => x.PlanPriceId,
                        principalSchema: "Multitenancy",
                        principalTable: "PlanPrices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Plans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "Multitenancy",
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivationTokenLookups_Token",
                schema: "Multitenancy",
                table: "ActivationTokenLookups",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLookups_IntegrationId",
                schema: "Multitenancy",
                table: "IntegrationLookups",
                column: "IntegrationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLookups_IntegrationType_ExternalIdentifier",
                schema: "Multitenancy",
                table: "IntegrationLookups",
                columns: new[] { "IntegrationType", "ExternalIdentifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanFeatures_PlanId_FeatureKey",
                schema: "Multitenancy",
                table: "PlanFeatures",
                columns: new[] { "PlanId", "FeatureKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanLimits_PlanId_LimitKey",
                schema: "Multitenancy",
                table: "PlanLimits",
                columns: new[] { "PlanId", "LimitKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanPrices_PlanId_BillingInterval_IntervalCount",
                schema: "Multitenancy",
                table: "PlanPrices",
                columns: new[] { "PlanId", "BillingInterval", "IntervalCount" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plans_Identifier",
                schema: "Multitenancy",
                table: "Plans",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanId",
                schema: "Multitenancy",
                table: "Subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanPriceId",
                schema: "Multitenancy",
                table: "Subscriptions",
                column: "PlanPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId",
                schema: "Multitenancy",
                table: "Subscriptions",
                column: "TenantId",
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Identifier",
                schema: "Multitenancy",
                table: "Tenants",
                column: "Identifier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivationTokenLookups",
                schema: "Multitenancy");

            migrationBuilder.DropTable(
                name: "IntegrationLookups",
                schema: "Multitenancy");

            migrationBuilder.DropTable(
                name: "PlanFeatures",
                schema: "Multitenancy");

            migrationBuilder.DropTable(
                name: "PlanLimits",
                schema: "Multitenancy");

            migrationBuilder.DropTable(
                name: "Subscriptions",
                schema: "Multitenancy");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "Multitenancy");

            migrationBuilder.DropTable(
                name: "PlanPrices",
                schema: "Multitenancy");

            migrationBuilder.DropTable(
                name: "Plans",
                schema: "Multitenancy");
        }
    }
}
