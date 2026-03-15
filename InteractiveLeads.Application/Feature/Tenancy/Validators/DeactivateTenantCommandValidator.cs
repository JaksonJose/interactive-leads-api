using FluentValidation;
using InteractiveLeads.Application.Feature.Tenancy.Commands;

namespace InteractiveLeads.Application.Feature.Tenancy.Validators
{
    /// <summary>
    /// Validator for DeactivateTenantCommand requests.
    /// </summary>
    public class DeactivateTenantCommandValidator : AbstractValidator<DeactivateTenantCommand>
    {
        /// <summary>
        /// Initializes a new instance of the DeactivateTenantCommandValidator class.
        /// </summary>
        public DeactivateTenantCommandValidator()
        {
            RuleFor(request => request.TenantId)
                .NotEmpty()
                .WithMessage("tenancy.tenant_id_required:Tenant ID is required");
        }
    }
}
