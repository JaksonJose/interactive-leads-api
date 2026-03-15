using FluentValidation;
using InteractiveLeads.Application.Feature.Users.Commands;

namespace InteractiveLeads.Application.Feature.Users.Validators
{
    /// <summary>
    /// Validator for UpdateUserCommand requests.
    /// </summary>
    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        /// <summary>
        /// Initializes a new instance of the UpdateUserCommandValidator class.
        /// </summary>
        public UpdateUserCommandValidator()
        {
            RuleFor(request => request.UpdateUser)
                .NotNull()
                .WithMessage("users.update_user_required:User update data is required");

            When(request => request.UpdateUser != null, () =>
            {
                RuleFor(request => request.UpdateUser.Id)
                    .NotEqual(Guid.Empty)
                    .WithMessage("users.id_required:User ID is required");

                RuleFor(request => request.UpdateUser.FirstName)
                    .NotEmpty()
                    .WithMessage("users.first_name_required:First name is required")
                    .MaximumLength(100)
                    .WithMessage("users.first_name_max_length:First name must have a maximum of 100 characters");

                RuleFor(request => request.UpdateUser.LastName)
                    .NotEmpty()
                    .WithMessage("users.last_name_required:Last name is required")
                    .MaximumLength(100)
                    .WithMessage("users.last_name_max_length:Last name must have a maximum of 100 characters");

                When(request => !string.IsNullOrEmpty(request.UpdateUser.PhoneNumber), () =>
                {
                    RuleFor(request => request.UpdateUser.PhoneNumber)
                        .Matches(@"^[\+]?[1-9][\d]{0,15}$")
                        .WithMessage("users.phone_number_invalid:Phone number must have a valid format");
                });
            });
        }
    }
}
