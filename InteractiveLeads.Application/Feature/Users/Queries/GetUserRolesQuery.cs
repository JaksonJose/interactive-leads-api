using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Users.Queries
{
    public class GetUserRolesQuery : IRequest<IResponse>, IValidate
    {
        public Guid UserId { get; set; }
    }

    public class GetUserRolesQueryHandler(IUserService userService) : IRequestHandler<GetUserRolesQuery, IResponse>
    {
        private readonly IUserService _userService = userService;

        public async Task<IResponse> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
        {
            return await _userService.GetUserRolesAsync(request.UserId, cancellationToken);
        }
    }
}
