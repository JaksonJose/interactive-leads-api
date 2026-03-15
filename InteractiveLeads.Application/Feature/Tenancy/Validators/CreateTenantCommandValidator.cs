using FluentValidation;
using InteractiveLeads.Application.Feature.Tenancy.Commands;

namespace InteractiveLeads.Application.Feature.Tenancy.Validators
{
    /// <summary>
    /// Validator for CreateTenantCommand requests.
    /// </summary>
    public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
    {
        /// <summary>
        /// Initializes a new instance of the CreateTenantCommandValidator class.
        /// </summary>
        public CreateTenantCommandValidator()
        {
            RuleFor(request => request.CreateTenant)
                .NotNull()
                .WithMessage("tenancy.create_tenant_required:Tenant data is required");

            When(request => request.CreateTenant != null, () =>
            {
                RuleFor(request => request.CreateTenant.Name)
                    .NotEmpty()
                    .WithMessage("tenancy.name_required:Tenant name is required")
                    .MaximumLength(200)
                    .WithMessage("tenancy.name_max_length:Name must have a maximum of 200 characters");

                RuleFor(request => request.CreateTenant.Email)
                    .NotEmpty()
                    .WithMessage("tenancy.email_required:Administrator email is required")
                    .EmailAddress()
                    .WithMessage("tenancy.email_invalid:Email must have a valid format");

                RuleFor(request => request.CreateTenant.FirstName)
                    .NotEmpty()
                    .WithMessage("tenancy.first_name_required:Administrator first name is required")
                    .MaximumLength(100)
                    .WithMessage("tenancy.first_name_max_length:First name must have a maximum of 100 characters");

                RuleFor(request => request.CreateTenant.LastName)
                    .NotEmpty()
                    .WithMessage("tenancy.last_name_required:Administrator last name is required")
                    .MaximumLength(100)
                    .WithMessage("tenancy.last_name_max_length:Last name must have a maximum of 100 characters");

                RuleFor(request => request.CreateTenant.ExpirationDate)
                    .GreaterThan(DateTime.UtcNow)
                    .WithMessage("tenancy.expiration_date_future:Expiration date must be in the future");

                When(request => !string.IsNullOrEmpty(request.CreateTenant.ConnectionString), () =>
                {
                    RuleFor(request => request.CreateTenant.ConnectionString)
                        .Must(cs => IsValidConnectionString(cs))
                        .WithMessage("tenancy.connection_string_invalid:Connection string must be valid");
                });
            });
        }

        private static bool IsValidConnectionString(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return true;

            // Basic validation for connection string format
            return connectionString.Contains("Server=") || 
                   connectionString.Contains("Host=") || 
                   connectionString.Contains("Data Source=");
        }
    }
}
