using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Company> Companies { get; }
    DbSet<Inbox> Inboxes { get; }
    DbSet<InboxMember> InboxMembers { get; }
    DbSet<Integration> Integrations { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<ConversationAssignment> ConversationAssignments { get; }
    DbSet<ConversationInboxMovement> ConversationInboxMovements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

