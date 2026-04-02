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
/// Periodically finds open conversations whose first-response SLA has expired without an agent reply
/// and reassigns when the handling team has <see cref="Domain.Entities.Team.AutoReassignOnFirstResponseSlaExpired"/> enabled.
/// </summary>
public sealed class SlaFirstResponseReassignmentWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<SlaReassignmentWorkerSettings> options,
    ILogger<SlaFirstResponseReassignmentWorker> logger) : BackgroundService
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
                logger.LogError(ex, "SlaFirstResponseReassignmentWorker cycle failed.");
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

                    var eligibleTeamIds = await db.Teams
                        .AsNoTracking()
                        .Where(t => t.AutoAssignEnabled && t.AutoReassignOnFirstResponseSlaExpired && t.IsActive)
                        .Select(t => t.Id)
                        .ToListAsync(ct);

                    if (eligibleTeamIds.Count == 0)
                        return;

                    var rows = await db.Conversations
                        .AsNoTracking()
                        .Where(c =>
                            c.Status == ConversationStatus.Open &&
                            c.AssignedAgentId != null &&
                            c.FirstAgentResponseAt == null &&
                            c.FirstResponseDueAt != null &&
                            c.FirstResponseDueAt < utc &&
                            c.HandlingTeamId != null &&
                            eligibleTeamIds.Contains(c.HandlingTeamId.Value))
                        .Select(c => new { c.Id, c.CompanyId })
                        .Take(batchSize)
                        .ToListAsync(ct);

                    foreach (var row in rows)
                    {
                        try
                        {
                            await autoAssign.TryReassignAfterFirstResponseSlaExpiredAsync(
                                row.CompanyId,
                                tenant.Identifier,
                                row.Id,
                                ct);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(
                                ex,
                                "SLA reassign failed for conversation {ConversationId} in tenant {TenantId}",
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
