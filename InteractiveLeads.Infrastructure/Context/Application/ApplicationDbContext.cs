using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Infrastructure.Identity.Models;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Infrastructure.Context.Application
{
    public class ApplicationDbContext : BaseDbContext, IApplicationDbContext
    {
        public ApplicationDbContext(
            IMultiTenantContextAccessor<InteractiveTenantInfo> tenantContextAccessor,
            DbContextOptions<ApplicationDbContext> options)
            : base(tenantContextAccessor, options)
        {
        }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Inbox> Inboxes { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<SlaPolicy> SlaPolicies { get; set; }
        public DbSet<UserTeam> UserTeams { get; set; }
        public DbSet<InboxTeam> InboxTeams { get; set; }
        public DbSet<Integration> Integrations { get; set; }
        public DbSet<WhatsAppBusinessAccount> WhatsAppBusinessAccounts { get; set; }
        public DbSet<WhatsAppTemplate> WhatsAppTemplates { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<ContactChannel> ContactChannels { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<ConversationAssignment> ConversationAssignments { get; set; }
        public DbSet<ConversationInboxMovement> ConversationInboxMovements { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageMedia> MessageMedia { get; set; }
        public DbSet<MessageReaction> MessageReactions { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<UserActivationToken> ActivationTokens { get; set; }
    }
}
