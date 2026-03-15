using FluentValidation;
using InteractiveLeads.Application.Feature.Identity.Impersonation.Commands;

namespace InteractiveLeads.Application.Feature.Identity.Validators
{
    public class ImpersonateCommandValidator : AbstractValidator<ImpersonateCommand>
    {
        public ImpersonateCommandValidator()
        {
            RuleFor(c => c.Request)
                .NotNull()
                .WithMessage("auth.impersonate_request_required:Impersonation request is required");

            When(c => c.Request != null, () =>
            {
                RuleFor(c => c.Request!.UserId)
                    .NotEmpty()
                    .WithMessage("auth.impersonate_user_id_required:Target user id is required");
            });
        }
    }
}
