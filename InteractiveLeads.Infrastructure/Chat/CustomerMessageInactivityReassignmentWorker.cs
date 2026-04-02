using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Requests;
using InteractiveLeads.Domain.Enums;
using InteractiveLeads.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InteractiveLeads.Infrastructure.Chat;

/// <summary>
/// Reassigns open conversations where the last message is from the customer and older than the team's
/// configured inactivity timeout (minutes), after at least one agent reply.
/// </summary>
public sealed class CustomerMessageInactivityReassignmentWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<SlaReassignmentWorkerSettings> options,
    ILogger<CustomerMessageInactivityReassignmentWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(15, options.Value.IntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CustomerMessageInactivityReassignmentWorker cycle failed.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        await using var outerScope = scopeFactory.CreateAsyncScope();
        var tenantService = outerScope.ServiceProvider.GetRequiredService<ITenantService>();
        var crossTenant = outerScope.ServiceProvider.GetRequiredService<ICrossTenantService>();

        var page = 1;
        const int tenantPageSize = 100;
        var batchSize = Math.Max(1, options.Value.BatchSize);

        while (!ct.IsCancellationRequested)
        {
            var tenantList = await tenantService.GetTenantsAsync(
                new InquiryRequest { Page = page, PageSize = tenantPageSize },
                ct);

            if (tenantList.Items is null || tenantList.Items.Count == 0)
                break;

            foreach (var tenant in tenantList.Items)
            {
                if (!tenant.IsActive || string.IsNullOrWhiteSpace(tenant.Identifier))
                    continue;

                await crossTenant.ExecuteInTenantContextForSystemAsync(tenant.Identifier, async sp =>
                {
                    var db = sp.GetRequiredService<IApplicationDbContext>();
                    var autoAssign = sp.GetRequiredService<IConversationAutoAssignService>();

                    var utc = DateTimeOffset.UtcNow;

                    var rows = await (
                        from c in db.Conversations.AsNoTracking()
                        join t in db.Teams.AsNoTracking() on c.HandlingTeamId equals t.Id
                        where c.Status == ConversationStatus.Open
                              && c.AssignedAgentId != null
                              && c.FirstAgentResponseAt != null
                              && c.LastMessageFromCustomer
                              && t.IsActive
                              && t.AutoAssignEnabled
                              && t.AutoAssignReassignTimeoutMinutes != null
                              && t.AutoAssignReassignTimeoutMinutes > 0
                              && c.LastMessageAt.AddMinutes(t.AutoAssignReassignTimeoutMinutes.Value) <= utc
                        select new { c.Id, c.CompanyId }
                    ).Take(batchSize).ToListAsync(ct);

                    foreach (var row in rows)
                    {
                        try
                        {
                            await autoAssign.TryReassignAfterCustomerMessageInactivityAsync(
                                row.CompanyId,
                                tenant.Identifier,
                                row.Id,
                                ct);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(
                                ex,
                                "Inactivity reassign failed for conversation {ConversationId} in tenant {TenantId}",
                                row.Id,
                                tenant.Identifier);
                        }
                    }
                });
            }

            if (tenantList.Items.Count < tenantPageSize)
                break;

            page++;
        }
    }
}
