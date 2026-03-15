using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Identity.Tokens.Queries
{
    /// <summary>
    /// Query for refreshing an expired JWT access token.
    /// </summary>
    /// <remarks>
    /// This query implements the CQRS pattern for token refresh operations,
    /// allowing users to obtain new tokens without re-authenticating.
    /// </remarks>
    public class GetRefreshTokenQuery : IRequest<IResponse>, IValidate
    {
        /// <summary>
        /// Gets or sets the refresh token request containing current tokens.
        /// </summary>
        public RefreshTokenRequest RefreshToken { get; set; } = new();
    }

    /// <summary>
    /// Handler for processing GetRefreshTokenQuery requests.
    /// </summary>
    /// <remarks>
    /// Validates the refresh token via ITokenService and returns new JWT tokens.
    /// </remarks>
    public class GetRefreshTokenQueryHandler : IRequestHandler<GetRefreshTokenQuery, IResponse>
    {
        private readonly ITokenService _tokenService;

        /// <summary>
        /// Initializes a new instance of the GetRefreshTokenQueryHandler class.
        /// </summary>
        /// <param name="tokenService">The token service for refresh token operations.</param>
        public GetRefreshTokenQueryHandler(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        /// <summary>
        /// Handles the GetRefreshTokenQuery request and generates new tokens.
        /// </summary>
        /// <param name="request">The query containing the refresh token information.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A wrapped response containing the new JWT tokens if refresh succeeds.</returns>
        public async Task<IResponse> Handle(GetRefreshTokenQuery request, CancellationToken cancellationToken)
        {
            return await _tokenService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
        }
    }
}
