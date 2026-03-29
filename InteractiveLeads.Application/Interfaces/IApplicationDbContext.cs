using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace InteractiveLeads.Application.Interfaces;

public interface IApplicationDbContext
{
    DatabaseFacade Database { get; }

    DbSet<Tenant> Tenants { get; }
    DbSet<Company> Companies { get; }
    DbSet<Inbox> Inboxes { get; }
    DbSet<InboxMember> InboxMembers { get; }
    DbSet<Integration> Integrations { get; }
    DbSet<WhatsAppBusinessAccount> WhatsAppBusinessAccounts { get; }
    DbSet<WhatsAppTemplate> WhatsAppTemplates { get; }
    DbSet<Contact> Contacts { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<ConversationAssignment> ConversationAssignments { get; }
    DbSet<ConversationInboxMovement> ConversationInboxMovements { get; }
    DbSet<Message> Messages { get; }
    DbSet<MessageMedia> MessageMedia { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

