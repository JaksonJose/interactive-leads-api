using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InteractiveLeads.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Identity");

            migrationBuilder.EnsureSchema(
                name: "Crm");

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

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
                name: "Users",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "ID of the tenant to which this user belongs; NULL for global users"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeviceInfo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                schema: "Identity",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "Identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "Identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                schema: "Identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
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
                name: "Inbox",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inbox_Company_CompanyId",
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
                name: "InboxMember",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CanBeAssigned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InboxMember_Inbox_InboxId",
                        column: x => x.InboxId,
                        principalSchema: "Crm",
                        principalTable: "Inbox",
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
                    InboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssignedAgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
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
                        name: "FK_Conversation_Inbox_InboxId",
                        column: x => x.InboxId,
                        principalSchema: "Crm",
                        principalTable: "Inbox",
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
                name: "ConversationAssignment",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AssignedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UnassignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationAssignment_Conversation_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "Crm",
                        principalTable: "Conversation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConversationInboxMovement",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromInboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToInboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    MovedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    MovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationInboxMovement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationInboxMovement_Conversation_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "Crm",
                        principalTable: "Conversation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConversationParticipant",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationParticipant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationParticipant_Contact_ContactId",
                        column: x => x.ContactId,
                        principalSchema: "Crm",
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConversationParticipant_Conversation_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "Crm",
                        principalTable: "Conversation",
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
                name: "IX_Conversation_InboxId",
                schema: "Crm",
                table: "Conversation",
                column: "InboxId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_IntegrationId",
                schema: "Crm",
                table: "Conversation",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_LastMessageAt",
                schema: "Crm",
                table: "Conversation",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_Priority",
                schema: "Crm",
                table: "Conversation",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_Status",
                schema: "Crm",
                table: "Conversation",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationAssignment_AssignedAt",
                schema: "Crm",
                table: "ConversationAssignment",
                column: "AssignedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationAssignment_ConversationId",
                schema: "Crm",
                table: "ConversationAssignment",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationAssignment_ConversationId_UserId_Active",
                schema: "Crm",
                table: "ConversationAssignment",
                columns: new[] { "ConversationId", "UserId" },
                unique: true,
                filter: "\"UnassignedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationAssignment_UserId",
                schema: "Crm",
                table: "ConversationAssignment",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationInboxMovement_ConversationId",
                schema: "Crm",
                table: "ConversationInboxMovement",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationInboxMovement_MovedAt",
                schema: "Crm",
                table: "ConversationInboxMovement",
                column: "MovedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationInboxMovement_ToInboxId",
                schema: "Crm",
                table: "ConversationInboxMovement",
                column: "ToInboxId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_ContactId",
                schema: "Crm",
                table: "ConversationParticipant",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_ConversationId",
                schema: "Crm",
                table: "ConversationParticipant",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_ConversationId_ContactId_Active",
                schema: "Crm",
                table: "ConversationParticipant",
                columns: new[] { "ConversationId", "ContactId" },
                unique: true,
                filter: "\"IsActive\" = true AND \"ContactId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_ConversationId_UserId_Active",
                schema: "Crm",
                table: "ConversationParticipant",
                columns: new[] { "ConversationId", "UserId" },
                unique: true,
                filter: "\"IsActive\" = true AND \"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_IsActive",
                schema: "Crm",
                table: "ConversationParticipant",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_UserId",
                schema: "Crm",
                table: "ConversationParticipant",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Inbox_CompanyId",
                schema: "Crm",
                table: "Inbox",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Inbox_IsActive",
                schema: "Crm",
                table: "Inbox",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMember_InboxId",
                schema: "Crm",
                table: "InboxMember",
                column: "InboxId");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMember_InboxId_UserId_Active",
                schema: "Crm",
                table: "InboxMember",
                columns: new[] { "InboxId", "UserId" },
                unique: true,
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMember_IsActive",
                schema: "Crm",
                table: "InboxMember",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMember_UserId",
                schema: "Crm",
                table: "InboxMember",
                column: "UserId");

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
                name: "UX_Message_ExternalMessageId",
                schema: "Crm",
                table: "Message",
                column: "ExternalMessageId",
                unique: true);

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
                name: "IX_RefreshTokens_ExpirationTime",
                schema: "Identity",
                table: "RefreshTokens",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                schema: "Identity",
                table: "RefreshTokens",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                schema: "Identity",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_NormalizedName",
                schema: "Identity",
                table: "Roles",
                column: "NormalizedName",
                filter: "\"NormalizedName\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "Identity",
                table: "Roles",
                columns: new[] { "NormalizedName", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenant_Identifier",
                schema: "Crm",
                table: "Tenant",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                schema: "Identity",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                schema: "Identity",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "Identity",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "Identity",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                schema: "Identity",
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Email",
                schema: "Identity",
                table: "Users",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "Identity",
                table: "Users",
                columns: new[] { "NormalizedUserName", "TenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivationTokens",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "ContactChannel",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "ConversationAssignment",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "ConversationInboxMovement",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "ConversationParticipant",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "InboxMember",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "MessageMedia",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "MessageReaction",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "UserClaims",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "UserLogins",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "UserTokens",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "Message",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "Conversation",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "Contact",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "Inbox",
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
