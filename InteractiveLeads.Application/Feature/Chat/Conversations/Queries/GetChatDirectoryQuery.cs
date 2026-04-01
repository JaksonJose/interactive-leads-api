using System.Globalization;
using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Realtime.Services.Presence;
using InteractiveLeads.Application.Responses;

namespace InteractiveLeads.Application.Feature.Chat.Conversations.Queries;

public sealed class GetChatDirectoryQuery : IApplicationRequest<IResponse>
{
    public ChatDirectoryMode Mode { get; set; }
    public Guid? InboxId { get; set; }
}

public sealed class GetChatDirectoryQueryHandler(
    IChatUserDirectoryService directoryService,
    IPresenceService presenceService,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<GetChatDirectoryQuery, IResponse>
{
    public async Task<IResponse> Handle(GetChatDirectoryQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentUserService.GetUserTenant();
        if (string.IsNullOrWhiteSpace(tenantId))
            return new ListResponse<ChatDirectoryUserDto>([], 0);

        var rows = await directoryService.ListAsync(request.Mode, request.InboxId, cancellationToken);
        var presence = await presenceService.ListTenantPresenceAsync(tenantId, cancellationToken);
        var presenceByUser = presence.ToDictionary(
            p => NormalizeUserIdKey(p.UserId),
            p => p,
            StringComparer.OrdinalIgnoreCase);

        var items = new List<ChatDirectoryUserDto>(rows.Count);
        foreach (var row in rows)
        {
            presenceByUser.TryGetValue(NormalizeUserIdKey(row.UserId), out var state);
            items.Add(new ChatDirectoryUserDto
            {
                UserId = row.UserId,
                DisplayName = row.DisplayName,
                Email = row.Email,
                Roles = row.Roles.ToList(),
                IsOnline = state?.IsOnline ?? false,
                LastSeenAtUtc = state?.LastSeenAtUtc
            });
        }

        items.Sort((a, b) =>
        {
            var o = b.IsOnline.CompareTo(a.IsOnline);
            return o != 0 ? o : string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
        });

        return new ListResponse<ChatDirectoryUserDto>(items, items.Count);
    }

    private static string NormalizeUserIdKey(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return string.Empty;
        var t = userId.Trim();
        return Guid.TryParse(t, CultureInfo.InvariantCulture, out var g)
            ? g.ToString("D", CultureInfo.InvariantCulture)
            : t;
    }
}
