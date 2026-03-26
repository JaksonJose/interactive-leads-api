using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;

namespace InteractiveLeads.Application.Feature.Users.Commands
{
    public class UpdateUserRolesCommand : IApplicationRequest<IResponse>, IValidate
    {
        public Guid UserId { get; set; }
        public UserRolesRequest UserRolesRequest { get; set; }
    }

    public class UpdateUserRolesCommandHandler(IUserService userService) : IApplicationRequestHandler<UpdateUserRolesCommand, IResponse>
    {
        private readonly IUserService _userService = userService;

        public async Task<IResponse> Handle(UpdateUserRolesCommand request, CancellationToken cancellationToken)
        {
            return await _userService.AssignRolesAsync(request.UserId, request.UserRolesRequest);
        }
    }
}

