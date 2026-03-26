using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;

namespace InteractiveLeads.Application.Feature.Identity.Roles.Commands
{
    public class CreateRoleCommand : IApplicationRequest<IResponse>, IValidate
    {
        public CreateRoleRequest CreateRole { get; set; }
    }

    public class CreateRoleCommandHandler : IApplicationRequestHandler<CreateRoleCommand, IResponse>
    {
        private readonly IRoleService _roleService;

        public CreateRoleCommandHandler(IRoleService roleService)
        {
            _roleService = roleService;
        }

        public async Task<IResponse> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            return await _roleService.CreateAsync(request.CreateRole, cancellationToken);
        }
    }
}

