using FluentValidation;
using InteractiveLeads.Application.Feature.Users.Commands;

namespace InteractiveLeads.Application.Feature.Users.Validators
{
    /// <summary>
    /// Validator for ChangeUserPasswordCommand requests.
    /// </summary>
    public class ChangeUserPasswordCommandValidator : AbstractValidator<ChangeUserPasswordCommand>
    {
        /// <summary>
        /// Initializes a new instance of the ChangeUserPasswordCommandValidator class.
        /// </summary>
        public ChangeUserPasswordCommandValidator()
        {
            RuleFor(request => request.ChangePassword)
                .NotNull()
                .WithMessage("users.change_password_required:Password change data is required");

            When(request => request.ChangePassword != null, () =>
            {
                RuleFor(request => request.ChangePassword.UserId)
                    .NotEqual(Guid.Empty)
                    .WithMessage("users.user_id_required:User ID is required");

                RuleFor(request => request.ChangePassword.CurrentPassword)
                    .NotEmpty()
                    .WithMessage("users.current_password_required:Current password is required");

                RuleFor(request => request.ChangePassword.NewPassword)
                    .NotEmpty()
                    .WithMessage("users.new_password_required:New password is required")
                    .MinimumLength(6)
                    .WithMessage("users.new_password_min_length:New password must have at least 6 characters");

                RuleFor(request => request.ChangePassword.ConfirmNewPassword)
                    .NotEmpty()
                    .WithMessage("users.confirm_new_password_required:New password confirmation is required")
                    .Equal(request => request.ChangePassword.NewPassword)
                    .WithMessage("users.new_passwords_not_match:New passwords do not match");
            });
        }
    }
}
