using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;

namespace InteractiveLeads.Application.Feature.Users.Commands
{
    public class UpdateUserStatusCommand : IApplicationRequest<IResponse>, IValidate
    {
        public ChangeUserStatusRequest ChangeUserStatus { get; set; }
    }

    public class UpdateUserStatusCommandHandler(IUserService userService)
        : IApplicationRequestHandler<UpdateUserStatusCommand, IResponse>
    {
        private readonly IUserService _userService = userService;

        public async Task<IResponse> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
        {
            return await _userService.ActivateOrDeactivateAsync(request.ChangeUserStatus.UserId, request.ChangeUserStatus.Activation);
        }
    }
}

