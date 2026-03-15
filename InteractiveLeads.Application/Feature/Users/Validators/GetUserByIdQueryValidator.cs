using FluentValidation;
using InteractiveLeads.Application.Feature.Users.Queries;

namespace InteractiveLeads.Application.Feature.Users.Validators
{
    /// <summary>
    /// Validator for GetUserByIdQuery requests.
    /// </summary>
    public class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
    {
        /// <summary>
        /// Initializes a new instance of the GetUserByIdQueryValidator class.
        /// </summary>
        public GetUserByIdQueryValidator()
        {
            RuleFor(request => request.UserId)
                .NotEqual(Guid.Empty)
                .WithMessage("users.user_id_required:User ID is required");
        }
    }
}
