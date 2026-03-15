using FluentValidation;
using InteractiveLeads.Application.Feature.Users.Commands;

namespace InteractiveLeads.Application.Feature.Users.Validators
{
    /// <summary>
    /// Validator for CreateUserCommand requests.
    /// </summary>
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        /// <summary>
        /// Initializes a new instance of the CreateUserCommandValidator class.
        /// </summary>
        public CreateUserCommandValidator()
        {
            RuleFor(request => request.CreateUser)
                .NotNull()
                .WithMessage("users.create_user_required:User data is required");

            When(request => request.CreateUser != null, () =>
            {
                RuleFor(request => request.CreateUser.FirstName)
                    .NotEmpty()
                    .WithMessage("users.first_name_required:First name is required")
                    .MaximumLength(100)
                    .WithMessage("users.first_name_max_length:First name must have a maximum of 100 characters");

                RuleFor(request => request.CreateUser.LastName)
                    .NotEmpty()
                    .WithMessage("users.last_name_required:Last name is required")
                    .MaximumLength(100)
                    .WithMessage("users.last_name_max_length:Last name must have a maximum of 100 characters");

                RuleFor(request => request.CreateUser.Email)
                    .NotEmpty()
                    .WithMessage("users.email_required:Email is required")
                    .EmailAddress()
                    .WithMessage("users.email_invalid:Email must have a valid format");

                RuleFor(request => request.CreateUser.Password)
                    .NotEmpty()
                    .WithMessage("users.password_required:Password is required")
                    .MinimumLength(6)
                    .WithMessage("users.password_min_length:Password must have at least 6 characters");

                RuleFor(request => request.CreateUser.ConfirmPassword)
                    .NotEmpty()
                    .WithMessage("users.confirm_password_required:Password confirmation is required")
                    .Equal(request => request.CreateUser.Password)
                    .WithMessage("users.passwords_not_match:Passwords do not match");

                When(request => !string.IsNullOrEmpty(request.CreateUser.PhoneNumber), () =>
                {
                    RuleFor(request => request.CreateUser.PhoneNumber)
                        .Matches(@"^[\+]?[1-9][\d]{0,15}$")
                        .WithMessage("users.phone_number_invalid:Phone number must have a valid format");
                });
            });
        }
    }
}
