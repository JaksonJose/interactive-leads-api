using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;

namespace InteractiveLeads.Application.Feature.Identity.Roles.Commands
{
    public class DeleteRoleCommand : IApplicationRequest<IResponse>, IValidate
    {
        public Guid RoleId { get; set; }
    }

    public class DeleteRoleCommandHandler(IRoleService roleService) : IApplicationRequestHandler<DeleteRoleCommand, IResponse>
    {
        private readonly IRoleService _roleService = roleService;

        public async Task<IResponse> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            return await _roleService.DeleteAsync(request.RoleId, cancellationToken);
        }
    }
}

