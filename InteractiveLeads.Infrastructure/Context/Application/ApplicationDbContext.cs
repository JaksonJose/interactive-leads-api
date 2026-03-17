using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Infrastructure.Identity.Models;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Infrastructure.Context.Application
{
    public class ApplicationDbContext : BaseDbContext
    {
        public ApplicationDbContext(
            IMultiTenantContextAccessor<InteractiveTenantInfo> tenantContextAccessor,
            DbContextOptions<ApplicationDbContext> options)
            : base(tenantContextAccessor, options)
        {
        }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Integration> Integrations { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<ContactChannel> ContactChannels { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageMedia> MessageMedia { get; set; }
        public DbSet<MessageReaction> MessageReactions { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<UserActivationToken> ActivationTokens { get; set; }
    }
}
