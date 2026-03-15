using FluentValidation;
using InteractiveLeads.Application.Feature.Users.Commands;

namespace InteractiveLeads.Application.Feature.Users.Validators
{
    /// <summary>
    /// Validator for UpdateUserStatusCommand requests.
    /// </summary>
    public class UpdateUserStatusCommandValidator : AbstractValidator<UpdateUserStatusCommand>
    {
        /// <summary>
        /// Initializes a new instance of the UpdateUserStatusCommandValidator class.
        /// </summary>
        public UpdateUserStatusCommandValidator()
        {
            RuleFor(request => request.ChangeUserStatus)
                .NotNull()
                .WithMessage("users.change_user_status_required:User status change data is required");

            When(request => request.ChangeUserStatus != null, () =>
            {
                RuleFor(request => request.ChangeUserStatus.UserId)
                    .NotEqual(Guid.Empty)
                    .WithMessage("users.user_id_required:User ID is required");
            });
        }
    }
}
