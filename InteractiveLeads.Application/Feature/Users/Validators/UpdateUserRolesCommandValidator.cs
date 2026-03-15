using FluentValidation;
using InteractiveLeads.Application.Feature.Users.Commands;

namespace InteractiveLeads.Application.Feature.Users.Validators
{
    /// <summary>
    /// Validator for UpdateUserRolesCommand requests.
    /// </summary>
    public class UpdateUserRolesCommandValidator : AbstractValidator<UpdateUserRolesCommand>
    {
        /// <summary>
        /// Initializes a new instance of the UpdateUserRolesCommandValidator class.
        /// </summary>
        public UpdateUserRolesCommandValidator()
        {
            RuleFor(request => request.UserId)
                .NotEqual(Guid.Empty)
                .WithMessage("users.user_id_required:User ID is required");

            RuleFor(request => request.UserRolesRequest)
                .NotNull()
                .WithMessage("users.user_roles_request_required:User roles request data is required");

            When(request => request.UserRolesRequest != null, () =>
            {
                RuleFor(request => request.UserRolesRequest.UserRoles)
                    .NotEmpty()
                    .WithMessage("users.user_roles_list_required:User roles list cannot be empty");
            });
        }
    }
}
