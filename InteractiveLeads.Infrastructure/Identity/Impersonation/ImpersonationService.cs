using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Identity.Tokens;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Infrastructure.Constants;
using InteractiveLeads.Infrastructure.Tenancy.Extensions;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.Extensions.Logging;

namespace InteractiveLeads.Infrastructure.Identity.Impersonation
{
    /// <summary>
    /// Allows SysAdmin/Support to issue a token as another user (impersonation). Logs for audit.
    /// </summary>
    public class ImpersonationService : IImpersonationService
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserLookupService _userLookupService;
        private readonly ICrossTenantAuthorizationService _crossTenantAuthorization;
        private readonly IMultiTenantStore<InteractiveTenantInfo> _tenantStore;
        private readonly IMultiTenantContextSetter _tenantContextSetter;
        private readonly ITokenService _tokenService;
        private readonly ILogger<ImpersonationService> _logger;

        public ImpersonationService(
            ICurrentUserService currentUserService,
            IUserLookupService userLookupService,
            ICrossTenantAuthorizationService crossTenantAuthorization,
            IMultiTenantStore<InteractiveTenantInfo> tenantStore,
            IMultiTenantContextSetter tenantContextSetter,
            ITokenService tokenService,
            ILogger<ImpersonationService> logger)
        {
            _currentUserService = currentUserService;
            _userLookupService = userLookupService;
            _crossTenantAuthorization = crossTenantAuthorization;
            _tenantStore = tenantStore;
            _tenantContextSetter = tenantContextSetter;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<SingleResponse<TokenResponse>> ImpersonateAsync(Guid targetUserId, CancellationToken ct = default)
        {
            if (!_currentUserService.IsAuthenticated())
            {
                var unauth = new ResultResponse();
                unauth.AddErrorMessage("Authentication required.", "auth.unauthorized");
                throw new UnauthorizedException(unauth);
            }

            var allowed = RoleConstants.CrossTenantRoles.Any(role => _currentUserService.IsInRole(role));
            if (!allowed)
            {
                var forbidden = new ResultResponse();
                forbidden.AddErrorMessage("Only SysAdmin or Support can impersonate users.", "auth.impersonation_forbidden");
                throw new ForbiddenException(forbidden);
            }

            if (!Guid.TryParse(_currentUserService.GetUserId(), out var currentUserId))
            {
                var bad = new ResultResponse();
                bad.AddErrorMessage("Invalid current user.", "auth.invalid_user");
                throw new UnauthorizedException(bad);
            }

            var targetUser = await _userLookupService.GetUserByIdAsync(targetUserId, ct);
            if (targetUser == null)
            {
                var notFound = new ResultResponse();
                notFound.AddErrorMessage("Target user not found.", "auth.impersonation_target_not_found");
                throw new NotFoundException(notFound);
            }

            var tenantId = targetUser.TenantId ?? TenancyConstants.GlobalTenantIdentifier;
            var canAccess = await _crossTenantAuthorization.CanAccessTenantAsync(currentUserId, tenantId);
            if (!canAccess)
            {
                var forbidden = new ResultResponse();
                forbidden.AddErrorMessage("You do not have access to the target user's tenant.", "auth.impersonation_tenant_forbidden");
                throw new ForbiddenException(forbidden);
            }

            var tenantInfo = await _tenantStore.TryGetAsync(tenantId);
            if (tenantInfo == null)
            {
                var notFound = new ResultResponse();
                notFound.AddErrorMessage("Tenant not found.", "auth.tenant_not_found");
                throw new NotFoundException(notFound);
            }

            _tenantContextSetter.MultiTenantContext = new MultiTenantContext<InteractiveTenantInfo>(tenantInfo);

            _logger.LogInformation(
                "Impersonation: User {CurrentUserId} is impersonating user {TargetUserId} (tenant: {TenantId})",
                currentUserId, targetUserId, tenantId);

            return await _tokenService.GenerateTokenForImpersonationAsync(targetUser, currentUserId, ct);
        }
    }
}
