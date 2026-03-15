using FluentValidation;
using InteractiveLeads.Application.Feature.Identity.Tokens.Queries;
using InteractiveLeads.Application.Interfaces;

namespace InteractiveLeads.Application.Feature.Identity.Validators
{
    public class GetRefreshTokenQueryValidator : AbstractValidator<GetRefreshTokenQuery>
    {
        public GetRefreshTokenQueryValidator(ITokenService tokenService)
        {
            RuleFor(request => request.RefreshToken)
                .NotNull()
                .WithMessage("auth.refresh_token_data_required:Refresh token data is required");

            When(request => request.RefreshToken != null, () =>
            {
                RuleFor(request => request.RefreshToken.CurrentJwt)
                    .NotEmpty()
                    .WithMessage("auth.current_jwt_required:Current JWT token is required");

                RuleFor(request => request.RefreshToken.CurrentRefreshToken)
                    .NotEmpty()
                    .WithMessage("auth.current_refresh_token_required:Current refresh token is required");
            });
        }
    }
}
