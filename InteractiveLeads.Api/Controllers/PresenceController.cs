using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Realtime.Services.Presence;
using InteractiveLeads.Application.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using System.Security.Claims;

namespace InteractiveLeads.Api.Controllers;

[Authorize(Roles = "Owner,Manager,Agent")]
public sealed class PresenceController(IPresenceService presence) : BaseApiController
{
    private readonly IPresenceService _presence = presence;

    /// <summary>Snapshot of online/offline presence for the current tenant.</summary>
    [HttpGet("online")]
    [OpenApiOperation("List online/offline presence states")]
    public async Task<IActionResult> ListOnlineAsync(CancellationToken cancellationToken)
    {
        var tenantId = HttpContext?.Request?.Headers["tenant"].ToString();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            // Some clients rely on tenant claim (e.g. SignalR) instead of header.
            tenantId = User?.FindFirst("tenant")?.Value ?? User?.FindFirst("tenantId")?.Value;
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            // Tenant middleware uses header strategy; without tenant header we can't scope presence safely.
            return Ok(new ListResponse<PresenceStateDto>(new List<PresenceStateDto>())
                .AddErrorMessage("Missing tenant header.", "tenant.missing"));
        }

        var list = await _presence.ListTenantPresenceAsync(tenantId!, cancellationToken);
        return Ok(new ListResponse<PresenceStateDto>(list.ToList(), list.Count));
    }
}

