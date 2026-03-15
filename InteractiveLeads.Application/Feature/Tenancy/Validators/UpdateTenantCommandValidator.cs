using FluentValidation;
using InteractiveLeads.Application.Feature.Tenancy.Commands;

namespace InteractiveLeads.Application.Feature.Tenancy.Validators
{
    /// <summary>
    /// Validator for UpdateTenantCommand to ensure request data is valid.
    /// </summary>
    public sealed class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
    {
        /// <summary>
        /// Initializes a new instance of the UpdateTenantCommandValidator class.
        /// </summary>
        public UpdateTenantCommandValidator()
        {
            RuleFor(request => request.UpdateTenant)
                .NotNull()
                .WithMessage("Update tenant request cannot be null");

            When(request => request.UpdateTenant != null, () =>
            {
                RuleFor(request => request.UpdateTenant.Identifier)
                    .NotEmpty()
                    .WithMessage("Tenant identifier is required");

                RuleFor(request => request.UpdateTenant.Name)
                    .NotEmpty()
                    .WithMessage("Tenant name is required")
                    .MinimumLength(2)
                    .WithMessage("Tenant name must be at least 2 characters long")
                    .MaximumLength(100)
                    .WithMessage("Tenant name must not exceed 100 characters");

                RuleFor(request => request.UpdateTenant.Email)
                    .NotEmpty()
                    .WithMessage("Email is required")
                    .EmailAddress()
                    .WithMessage("Invalid email format");

                RuleFor(request => request.UpdateTenant.FirstName)
                    .NotEmpty()
                    .WithMessage("First name is required")
                    .MinimumLength(2)
                    .WithMessage("First name must be at least 2 characters long")
                    .MaximumLength(50)
                    .WithMessage("First name must not exceed 50 characters");

                RuleFor(request => request.UpdateTenant.LastName)
                    .NotEmpty()
                    .WithMessage("Last name is required")
                    .MinimumLength(2)
                    .WithMessage("Last name must be at least 2 characters long")
                    .MaximumLength(50)
                    .WithMessage("Last name must not exceed 50 characters");

                RuleFor(request => request.UpdateTenant.ExpirationDate)
                    .NotEmpty()
                    .WithMessage("Expiration date is required")
                    .Must(date => date > DateTime.UtcNow)
                    .WithMessage("Expiration date must be in the future");
            });
        }
    }
}

