using InteractiveLeads.Application.Feature.Users;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;

namespace Application.Features.Identity.Users.Queries
{
    public class GetAllUsersQuery : IApplicationRequest<IResponse>
    {
    }

    public class GetAllUsersQueryHandler(IUserService userService) : IApplicationRequestHandler<GetAllUsersQuery, IResponse>
    {
        private readonly IUserService _userService = userService;

        public async Task<IResponse> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            return await _userService.GetAllAsync(cancellationToken);
        }
    }
}

