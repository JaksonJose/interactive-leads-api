using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;

namespace InteractiveLeads.Application.Feature.Users.Commands
{
    public class ChangeUserPasswordCommand : IApplicationRequest<IResponse>, IValidate
    {
        public ChangePasswordRequest ChangePassword { get; set; }
    }

    public class ChangeUserPasswordCommandHandler(IUserService userService) :
        IApplicationRequestHandler<ChangeUserPasswordCommand, IResponse>
    {
        private readonly IUserService _userService = userService;

        public async Task<IResponse> Handle(ChangeUserPasswordCommand request, CancellationToken cancellationToken)
        {
            return await _userService.ChangePasswordAsync(request.ChangePassword);
        }
    }
}

