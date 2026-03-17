using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCrmDomainModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Crm");

            migrationBuilder.CreateTable(
                name: "Tenant",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Identifier = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Company",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Document = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Company", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Company_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "Crm",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contact",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contact_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "Crm",
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Integration",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExternalIdentifier = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Settings = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Integration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Integration_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "Crm",
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContactChannel",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactChannel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactChannel_Contact_ContactId",
                        column: x => x.ContactId,
                        principalSchema: "Crm",
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContactChannel_Integration_IntegrationId",
                        column: x => x.IntegrationId,
                        principalSchema: "Crm",
                        principalTable: "Integration",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Conversation",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssignedAgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastMessageAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversation_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "Crm",
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Conversation_Contact_ContactId",
                        column: x => x.ContactId,
                        principalSchema: "Crm",
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Conversation_Integration_IntegrationId",
                        column: x => x.IntegrationId,
                        principalSchema: "Crm",
                        principalTable: "Integration",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Message",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalMessageId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ReplyToMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    SenderUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Message", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Message_Conversation_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "Crm",
                        principalTable: "Conversation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Message_Message_ReplyToMessageId",
                        column: x => x.ReplyToMessageId,
                        principalSchema: "Crm",
                        principalTable: "Message",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessageMedia",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaType = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Caption = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageMedia_Message_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "Crm",
                        principalTable: "Message",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageReaction",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reaction = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageReaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageReaction_Message_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "Crm",
                        principalTable: "Message",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Company_TenantId",
                schema: "Crm",
                table: "Company",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_CompanyId",
                schema: "Crm",
                table: "Contact",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Phone",
                schema: "Crm",
                table: "Contact",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_ContactChannel_ContactId",
                schema: "Crm",
                table: "ContactChannel",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactChannel_Integration_ExternalId",
                schema: "Crm",
                table: "ContactChannel",
                columns: new[] { "IntegrationId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_AssignedAgentId",
                schema: "Crm",
                table: "Conversation",
                column: "AssignedAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_CompanyId",
                schema: "Crm",
                table: "Conversation",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_ContactId",
                schema: "Crm",
                table: "Conversation",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_IntegrationId",
                schema: "Crm",
                table: "Conversation",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_Integration_CompanyId",
                schema: "Crm",
                table: "Integration",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Integration_ExternalIdentifier",
                schema: "Crm",
                table: "Integration",
                column: "ExternalIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_Message_ConversationId",
                schema: "Crm",
                table: "Message",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Message_ReplyToMessageId",
                schema: "Crm",
                table: "Message",
                column: "ReplyToMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Message_SenderUserId",
                schema: "Crm",
                table: "Message",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageMedia_MessageId",
                schema: "Crm",
                table: "MessageMedia",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReaction_MessageId",
                schema: "Crm",
                table: "MessageReaction",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenant_Identifier",
                schema: "Crm",
                table: "Tenant",
                column: "Identifier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactChannel",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "MessageMedia",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "MessageReaction",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "Message",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "Conversation",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "Contact",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "Integration",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "Company",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "Tenant",
                schema: "Crm");
        }
    }
}
