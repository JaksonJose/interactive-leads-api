using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Identity.Tokens.Commands
{
    /// <summary>
    /// Command for logging out a user by revoking all their refresh tokens.
    /// </summary>
    /// <remarks>
    /// This command implements the CQRS pattern for user logout operations.
    /// It revokes all active refresh tokens for the specified user.
    /// </remarks>
    public class LogoutCommand : IRequest<IResponse>, IValidate
    {
        /// <summary>
        /// Gets or sets the user ID for whom to revoke refresh tokens.
        /// </summary>
        public Guid UserId { get; set; }
    }

    /// <summary>
    /// Handler for processing LogoutCommand requests.
    /// </summary>
    /// <remarks>
    /// Revokes all active refresh tokens for the specified user via ITokenService.
    /// </remarks>
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, IResponse>
    {
        private readonly ITokenService _tokenService;

        /// <summary>
        /// Initializes a new instance of the LogoutCommandHandler class.
        /// </summary>
        /// <param name="tokenService">The token service for logout operations.</param>
        public LogoutCommandHandler(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        /// <summary>
        /// Handles the LogoutCommand request and revokes all refresh tokens for the user.
        /// </summary>
        /// <param name="request">The command containing the user ID.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A wrapped response indicating success or failure.</returns>
        public async Task<IResponse> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            return await _tokenService.RevokeUserRefreshTokensAsync(request.UserId);
        }
    }
}
