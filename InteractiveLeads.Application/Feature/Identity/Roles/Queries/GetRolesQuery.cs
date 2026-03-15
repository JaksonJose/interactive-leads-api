
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Identity.Roles.Queries
{
    public class GetRolesQuery : IRequest<IResponse>
    {
    }

    public class GetRolesQueryHandler(IRoleService roleService) : IRequestHandler<GetRolesQuery, IResponse>
    {
        private readonly IRoleService _roleService = roleService;

        public async Task<IResponse> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {
            return await _roleService.GetAllAsync(cancellationToken);
        }
    }
}
