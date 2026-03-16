using System.Security.Cryptography;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Activation;
using InteractiveLeads.Application.Feature.Users;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InteractiveLeads.Infrastructure.Identity.Activation
{
    /// <summary>
    /// Service for creating invitations (user + token + activation URL) and activating accounts by token.
    /// </summary>
    public class UserActivationService : IUserActivationService
    {
        private const int TokenBytes = 32;
        private const int TokenValidityDays = 7;

        private readonly IUserService _userService;
        private readonly IActivationTokenRepository _tokenRepository;
        private readonly ICrossTenantService _crossTenantService;
        private readonly IUserLookupService _userLookupService;
        private readonly ActivationSettings _settings;

        public UserActivationService(
            IUserService userService,
            IActivationTokenRepository tokenRepository,
            ICrossTenantService crossTenantService,
            IUserLookupService userLookupService,
            IOptions<ActivationSettings> settings)
        {
            _userService = userService;
            _tokenRepository = tokenRepository;
            _crossTenantService = crossTenantService;
            _userLookupService = userLookupService;
            _settings = settings.Value;
        }

        public async Task<InviteUserResponse> CreateInvitationAsync(InviteUserRequest request, CancellationToken cancellationToken = default)
        {
            var userId = await _userService.CreateUserForInvitationAsync(request, cancellationToken);
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenBytes)).Replace("+", "-").Replace("/", "_").TrimEnd('=');
            var expiresAt = DateTime.UtcNow.AddDays(TokenValidityDays);
            await _tokenRepository.AddAsync(userId, token, expiresAt, cancellationToken);

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
            var tokenModel = await _tokenRepository.GetByTokenAsync(token, cancellationToken);
            if (tokenModel == null || tokenModel.Used || tokenModel.ExpiresAt < DateTime.UtcNow)
            {
                var response = new ResultResponse();
                response.AddErrorMessage("Invalid or expired activation token.", "activation.invalid_or_expired");
                throw new BadRequestException(response);
            }

            var user = await _userLookupService.GetUserByIdAsync(tokenModel.UserId, cancellationToken);
            if (user?.TenantId == null)
            {
                var response = new ResultResponse();
                response.AddErrorMessage("User or tenant not found.", "activation.user_not_found");
                throw new NotFoundException(response);
            }

            await _crossTenantService.ExecuteInTenantContextForSystemAsync(user.TenantId, async (serviceProvider) =>
            {
                var userService = serviceProvider.GetRequiredService<IUserService>();
                var activationTokenRepository = serviceProvider.GetRequiredService<IActivationTokenRepository>();
                await userService.SetPasswordAndActivateAsync(tokenModel.UserId, newPassword, cancellationToken);
                await activationTokenRepository.MarkAsUsedAsync(tokenModel.Id, cancellationToken);
            });
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

                var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenBytes))
                    .Replace("+", "-").Replace("/", "_").TrimEnd('=');
                var expiresAt = DateTime.UtcNow.AddDays(TokenValidityDays);
                await scopedTokenRepo.AddAsync(userId, token, expiresAt, cancellationToken);

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
