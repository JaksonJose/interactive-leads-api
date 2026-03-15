using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Identity.Roles.Queries
{
    public class GetRoleWithPermissionsQuery : IRequest<IResponse>, IValidate
    {
        public Guid RoleId { get; set; }
    }

    public class GetRoleWithPermissionsQueryHandler(IRoleService roleService) : IRequestHandler<GetRoleWithPermissionsQuery, IResponse>
    {
        private readonly IRoleService _roleService = roleService;

        public async Task<IResponse> Handle(GetRoleWithPermissionsQuery request, CancellationToken cancellationToken)
        {
            return await _roleService.GetRoleWithPermissionsAsync(request.RoleId, cancellationToken);
        }
    }
}
