using System.Security.Cryptography;
using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Activation;
using InteractiveLeads.Application.Feature.Users;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Infrastructure.Configuration;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InteractiveLeads.Infrastructure.Identity.Activation
{
    /// <summary>
    /// Service for creating invitations (user + token + activation URL) and activating accounts by token.
    /// Uses global token lookup (host DB) so activation works when tenants have dedicated databases.
    /// </summary>
    public class UserActivationService : IUserActivationService
    {
        private const int TokenBytes = 32;
        private const int TokenValidityDays = 7;

        private readonly IUserService _userService;
        private readonly IActivationTokenRepository _tokenRepository;
        private readonly IActivationTokenLookupRepository _tokenLookup;
        private readonly ICrossTenantService _crossTenantService;
        private readonly IUserLookupService _userLookupService;
        private readonly IMultiTenantContextAccessor<InteractiveTenantInfo> _tenantContextAccessor;
        private readonly ActivationSettings _settings;

        public UserActivationService(
            IUserService userService,
            IActivationTokenRepository tokenRepository,
            IActivationTokenLookupRepository tokenLookup,
            ICrossTenantService crossTenantService,
            IUserLookupService userLookupService,
            IMultiTenantContextAccessor<InteractiveTenantInfo> tenantContextAccessor,
            IOptions<ActivationSettings> settings)
        {
            _userService = userService;
            _tokenRepository = tokenRepository;
            _tokenLookup = tokenLookup;
            _crossTenantService = crossTenantService;
            _userLookupService = userLookupService;
            _tenantContextAccessor = tenantContextAccessor;
            _settings = settings.Value;
        }

        public async Task<InviteUserResponse> CreateInvitationAsync(InviteUserRequest request, CancellationToken cancellationToken = default)
        {
            var userId = await _userService.CreateUserForInvitationAsync(request, cancellationToken);
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenBytes)).Replace("+", "-").Replace("/", "_").TrimEnd('=');
            var expiresAt = DateTime.UtcNow.AddDays(TokenValidityDays);
            await _tokenRepository.AddAsync(userId, token, expiresAt, cancellationToken);

            var tenantId = _tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;
            if (!string.IsNullOrEmpty(tenantId))
                await _tokenLookup.AddAsync(token, tenantId, userId, expiresAt, cancellationToken);

            var baseUrl = ( _settings.FrontendBaseUrl ?? "http://localhost:4200" ).TrimEnd('/');
            var activationUrl = $"{baseUrl}/activate?token={Uri.EscapeDataString(token)}";

            return new InviteUserResponse
            {
                UserId = userId,
                ActivationUrl = activationUrl
            };
        }

        public async Task ActivateAccountAsync(string token, string newPassword, CancellationToken cancellationToken = default)
        {
            // Resolve token from global lookup (host DB) so we get TenantId when tenant has dedicated database.
            var lookup = await _tokenLookup.GetByTokenAsync(token, cancellationToken);
            if (lookup == null || lookup.Used || lookup.ExpiresAt < DateTime.UtcNow)
            {
                var response = new ResultResponse();
                response.AddErrorMessage("Invalid or expired activation token.", "activation.invalid_or_expired");
                throw new BadRequestException(response);
            }

            await _crossTenantService.ExecuteInTenantContextForSystemAsync(lookup.TenantId, async (serviceProvider) =>
            {
                var userService = serviceProvider.GetRequiredService<IUserService>();
                var activationTokenRepository = serviceProvider.GetRequiredService<IActivationTokenRepository>();
                await userService.SetPasswordAndActivateAsync(lookup.UserId, newPassword, cancellationToken);
                var tokenInTenant = await activationTokenRepository.GetByTokenAsync(token, cancellationToken);
                if (tokenInTenant != null)
                    await activationTokenRepository.MarkAsUsedAsync(tokenInTenant.Id, cancellationToken);
            });

            await _tokenLookup.MarkAsUsedAsync(token, cancellationToken);
        }

        public async Task<InviteUserResponse> ResendInvitationAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userLookupService.GetUserByIdAsync(userId, cancellationToken);
            if (user == null || user.TenantId == null)
            {
                var response = new ResultResponse();
                response.AddErrorMessage("User or tenant not found.", "activation.user_not_found");
                throw new NotFoundException(response);
            }

            // If user is already active, do not resend activation
            if (user is { } lookup && lookup.Roles != null)
            {
                // we cannot see IsActive via lookup, so rely on UserService inside tenant context
            }

            // Executa dentro do contexto do tenant em modo "system" (sem checagem de usuário atual),
            // pois a autorização já foi validada no command handler chamador.
            InviteUserResponse? result = null;

            await _crossTenantService.ExecuteInTenantContextForSystemAsync(user.TenantId, async serviceProvider =>
            {
                var scopedUserService = serviceProvider.GetRequiredService<IUserService>();
                var scopedTokenRepo = serviceProvider.GetRequiredService<IActivationTokenRepository>();

                var userResponse = await scopedUserService.GetByIdAsync(userId, cancellationToken);
                if (userResponse.Data == null)
                {
                    var notFound = new ResultResponse();
                    notFound.AddErrorMessage("User not found.", "activation.user_not_found");
                    throw new NotFoundException(notFound);
                }

                if (userResponse.Data.IsActive)
                {
                    var conflict = new ResultResponse();
                    conflict.AddErrorMessage("User is already active.", "activation.user_already_active");
                    throw new ConflictException(conflict);
                }

                await scopedTokenRepo.InvalidateTokensForUserAsync(userId, cancellationToken);
                await _tokenLookup.InvalidateForUserAsync(user.TenantId, userId, cancellationToken);

                var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenBytes))
                    .Replace("+", "-").Replace("/", "_").TrimEnd('=');
                var expiresAt = DateTime.UtcNow.AddDays(TokenValidityDays);
                await scopedTokenRepo.AddAsync(userId, token, expiresAt, cancellationToken);
                await _tokenLookup.AddAsync(token, user.TenantId, userId, expiresAt, cancellationToken);

                var baseUrl = (_settings.FrontendBaseUrl ?? "http://localhost:4200").TrimEnd('/');
                var activationUrl = $"{baseUrl}/activate?token={Uri.EscapeDataString(token)}";

                result = new InviteUserResponse
                {
                    UserId = userId,
                    ActivationUrl = activationUrl
                };
            });

            return result!;
        }
    }
}
