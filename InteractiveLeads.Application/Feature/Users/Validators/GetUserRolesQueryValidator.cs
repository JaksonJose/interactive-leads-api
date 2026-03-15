using FluentValidation;
using InteractiveLeads.Application.Feature.Users.Queries;

namespace InteractiveLeads.Application.Feature.Users.Validators
{
    /// <summary>
    /// Validator for GetUserRolesQuery requests.
    /// </summary>
    public class GetUserRolesQueryValidator : AbstractValidator<GetUserRolesQuery>
    {
        /// <summary>
        /// Initializes a new instance of the GetUserRolesQueryValidator class.
        /// </summary>
        public GetUserRolesQueryValidator()
        {
            RuleFor(request => request.UserId)
                .NotEqual(Guid.Empty)
                .WithMessage("users.user_id_required:User ID is required");
        }
    }
}
