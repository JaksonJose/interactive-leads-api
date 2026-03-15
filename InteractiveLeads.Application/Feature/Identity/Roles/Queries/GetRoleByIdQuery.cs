using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Identity.Roles.Queries
{
    public class GetRoleByIdQuery : IRequest<IResponse>, IValidate
    {
        public Guid RoleId { get; set; }
    }

    public class GetRoleByIdQueryHandler(IRoleService roleService) : IRequestHandler<GetRoleByIdQuery, IResponse>
    {
        private readonly IRoleService _roleService = roleService;

        public async Task<IResponse> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
        {
            return await _roleService.GetByIdAsync(request.RoleId, cancellationToken);
        }
    }
}
