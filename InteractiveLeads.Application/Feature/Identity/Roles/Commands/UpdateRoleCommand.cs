using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;

namespace InteractiveLeads.Application.Feature.Identity.Roles.Commands
{
    public class UpdateRoleCommand : IApplicationRequest<IResponse>, IValidate
    {
        public UpdateRoleRequest UpdateRole { get; set; }
    }

    public class UpdateRoleCommandHandler(IRoleService roleService) : IApplicationRequestHandler<UpdateRoleCommand, IResponse>
    {
        private readonly IRoleService _roleService = roleService;

        public async Task<IResponse> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            return await _roleService.UpdateAsync(request.UpdateRole, cancellationToken);
        }
    }
}

