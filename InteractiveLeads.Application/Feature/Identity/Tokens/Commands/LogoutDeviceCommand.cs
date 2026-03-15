using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Identity.Tokens.Commands
{
    /// <summary>
    /// Command for logging out a user from a specific device by revoking a specific refresh token.
    /// </summary>
    /// <remarks>
    /// This command implements the CQRS pattern for device-specific logout operations.
    /// It revokes only the specified refresh token, allowing the user to remain logged in on other devices.
    /// </remarks>
    public class LogoutDeviceCommand : IRequest<IResponse>, IValidate
    {
        /// <summary>
        /// Gets or sets the user ID for whom to revoke the specific refresh token.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the refresh token to revoke.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handler for processing LogoutDeviceCommand requests.
    /// </summary>
    /// <remarks>
    /// Revokes a specific refresh token for the specified user via ITokenService.
    /// </remarks>
    public class LogoutDeviceCommandHandler : IRequestHandler<LogoutDeviceCommand, IResponse>
    {
        private readonly ITokenService _tokenService;

        /// <summary>
        /// Initializes a new instance of the LogoutDeviceCommandHandler class.
        /// </summary>
        /// <param name="tokenService">The token service for device logout operations.</param>
        public LogoutDeviceCommandHandler(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        /// <summary>
        /// Handles the LogoutDeviceCommand request and revokes the specific refresh token.
        /// </summary>
        /// <param name="request">The command containing the user ID and refresh token.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A wrapped response indicating success or failure.</returns>
        public async Task<IResponse> Handle(LogoutDeviceCommand request, CancellationToken cancellationToken)
        {
            return await _tokenService.RevokeSpecificRefreshTokenAsync(request.UserId, request.RefreshToken);
        }
    }
}
