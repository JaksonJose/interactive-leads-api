
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;

namespace InteractiveLeads.Application.Feature.Identity.Roles.Queries
{
    public class GetRolesQuery : IApplicationRequest<IResponse>
    {
    }

    public class GetRolesQueryHandler(IRoleService roleService) : IApplicationRequestHandler<GetRolesQuery, IResponse>
    {
        private readonly IRoleService _roleService = roleService;

        public async Task<IResponse> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {
            return await _roleService.GetAllAsync(cancellationToken);
        }
    }
}

