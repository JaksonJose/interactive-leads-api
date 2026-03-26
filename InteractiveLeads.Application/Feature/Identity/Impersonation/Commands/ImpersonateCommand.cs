using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;

namespace InteractiveLeads.Application.Feature.Identity.Impersonation.Commands
{
    /// <summary>
    /// Command to impersonate a user (SysAdmin/Support only). Returns new JWT for the target user.
    /// </summary>
    public class ImpersonateCommand : IApplicationRequest<IResponse>, IValidate
    {
        public ImpersonateRequest Request { get; set; } = new();
    }

    /// <summary>
    /// Handler for ImpersonateCommand. Delegates to IImpersonationService.
    /// </summary>
    public class ImpersonateCommandHandler : IApplicationRequestHandler<ImpersonateCommand, IResponse>
    {
        private readonly IImpersonationService _impersonationService;

        public ImpersonateCommandHandler(IImpersonationService impersonationService)
        {
            _impersonationService = impersonationService;
        }

        public async Task<IResponse> Handle(ImpersonateCommand request, CancellationToken cancellationToken)
        {
            return await _impersonationService.ImpersonateAsync(request.Request.UserId, cancellationToken);
        }
    }
}

