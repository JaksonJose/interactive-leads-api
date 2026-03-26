using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;

namespace InteractiveLeads.Application.Feature.Activation.Commands
{
    /// <summary>
    /// Command to activate an account using the token from the invitation link.
    /// </summary>
    public sealed class ActivateAccountCommand : IApplicationRequest<IResponse>
    {
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handler for ActivateAccountCommand. Public (no tenant); uses IUserActivationService.
    /// </summary>
    public sealed class ActivateAccountCommandHandler : IApplicationRequestHandler<ActivateAccountCommand, IResponse>
    {
        private readonly IUserActivationService _activationService;

        public ActivateAccountCommandHandler(IUserActivationService activationService)
        {
            _activationService = activationService;
        }

        public async Task<IResponse> Handle(ActivateAccountCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                var bad = new ResultResponse();
                bad.AddErrorMessage("Token is required.");
                throw new BadRequestException(bad);
            }
            if (request.NewPassword != request.ConfirmPassword)
            {
                var bad = new ResultResponse();
                bad.AddErrorMessage("Passwords do not match.", "user.passwords_not_match");
                throw new ConflictException(bad);
            }
            await _activationService.ActivateAccountAsync(request.Token, request.NewPassword, cancellationToken);
            var response = new ResultResponse();
            response.AddSuccessMessage("Account activated successfully.", "user.activated_successfully");
            return response;
        }
    }
}

