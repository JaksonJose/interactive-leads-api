using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Identity.Roles.Commands
{
    public class DeleteRoleCommand : IRequest<IResponse>, IValidate
    {
        public Guid RoleId { get; set; }
    }

    public class DeleteRoleCommandHandler(IRoleService roleService) : IRequestHandler<DeleteRoleCommand, IResponse>
    {
        private readonly IRoleService _roleService = roleService;

        public async Task<IResponse> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            return await _roleService.DeleteAsync(request.RoleId, cancellationToken);
        }
    }
}
