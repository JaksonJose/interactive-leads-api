using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Users.Commands
{
    public class UpdateUserStatusCommand : IRequest<IResponse>, IValidate
    {
        public ChangeUserStatusRequest ChangeUserStatus { get; set; }
    }

    public class UpdateUserStatusCommandHandler(IUserService userService)
        : IRequestHandler<UpdateUserStatusCommand, IResponse>
    {
        private readonly IUserService _userService = userService;

        public async Task<IResponse> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
        {
            return await _userService.ActivateOrDeactivateAsync(request.ChangeUserStatus.UserId, request.ChangeUserStatus.Activation);
        }
    }
}
