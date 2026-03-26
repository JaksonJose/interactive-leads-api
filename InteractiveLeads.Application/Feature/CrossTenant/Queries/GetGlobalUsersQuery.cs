using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;

namespace InteractiveLeads.Application.Feature.CrossTenant.Queries
{
    /// <summary>
    /// Query for retrieving all global users (TenantId = null), i.e. SysAdmin and Support users.
    /// Available for SysAdmin and Support.
    /// </summary>
    public sealed class GetGlobalUsersQuery : IApplicationRequest<IResponse>
    {
    }

    /// <summary>
    /// Handler for GetGlobalUsersQuery.
    /// </summary>
    public sealed class GetGlobalUsersQueryHandler : IApplicationRequestHandler<GetGlobalUsersQuery, IResponse>
    {
        private readonly IUserService _userService;

        public GetGlobalUsersQueryHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<IResponse> Handle(GetGlobalUsersQuery request, CancellationToken cancellationToken)
        {
            return await _userService.GetGlobalUsersAsync(cancellationToken);
        }
    }
}

