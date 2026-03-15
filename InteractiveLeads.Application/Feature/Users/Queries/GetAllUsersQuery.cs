using InteractiveLeads.Application.Feature.Users;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace Application.Features.Identity.Users.Queries
{
    public class GetAllUsersQuery : IRequest<IResponse>
    {
    }

    public class GetAllUsersQueryHandler(IUserService userService) : IRequestHandler<GetAllUsersQuery, IResponse>
    {
        private readonly IUserService _userService = userService;

        public async Task<IResponse> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            return await _userService.GetAllAsync(cancellationToken);
        }
    }
}
