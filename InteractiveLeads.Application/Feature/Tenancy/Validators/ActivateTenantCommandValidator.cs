using FluentValidation;
using InteractiveLeads.Application.Feature.Tenancy.Commands;

namespace InteractiveLeads.Application.Feature.Tenancy.Validators
{
    /// <summary>
    /// Validator for ActivateTenantCommand requests.
    /// </summary>
    public class ActivateTenantCommandValidator : AbstractValidator<ActivateTenantCommand>
    {
        /// <summary>
        /// Initializes a new instance of the ActivateTenantCommandValidator class.
        /// </summary>
        public ActivateTenantCommandValidator()
        {
            RuleFor(request => request.TenantId)
                .NotEmpty()
                .WithMessage("tenancy.tenant_id_required:Tenant ID is required");
        }
    }
}
