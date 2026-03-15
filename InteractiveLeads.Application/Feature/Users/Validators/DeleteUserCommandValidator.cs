using FluentValidation;
using InteractiveLeads.Application.Feature.Users.Commands;

namespace InteractiveLeads.Application.Feature.Users.Validators
{
    /// <summary>
    /// Validator for DeleteUserCommand requests.
    /// </summary>
    public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
    {
        /// <summary>
        /// Initializes a new instance of the DeleteUserCommandValidator class.
        /// </summary>
        public DeleteUserCommandValidator()
        {
            RuleFor(request => request.UserId)
                .NotEqual(Guid.Empty)
                .WithMessage("users.user_id_required:User ID is required");
        }
    }
}
