using FluentValidation;
using InteractiveLeads.Application.Feature.Identity.Tokens.Queries;
using InteractiveLeads.Application.Interfaces;

namespace InteractiveLeads.Application.Feature.Identity.Validators
{
    public class GetTokenQueryValidator : AbstractValidator<GetTokenQuery>
    {
        public GetTokenQueryValidator(ITokenService tokenService)
        {
            RuleFor(request => request.TokenRequest)
                .NotNull()
                .WithMessage("auth.token_request_required:Authentication data is required");

            When(request => request.TokenRequest != null, () =>
            {
                RuleFor(request => request.TokenRequest.UserName)
                    .NotEmpty()
                    .WithMessage("auth.username_required:Username or email is required");

                RuleFor(request => request.TokenRequest.Password)
                    .NotEmpty()
                    .WithMessage("auth.password_required:Password is required");
            });
        }
    }
}
