using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;

namespace InteractiveLeads.Application.Feature.Users.Commands
{
    public class DeleteUserCommand : IApplicationRequest<IResponse>, IValidate
    {
        public Guid UserId { get; set; }
    }

    public class DeleteUserCommandHandler(IUserService userService)
        : IApplicationRequestHandler<DeleteUserCommand, IResponse>
    {
        private readonly IUserService _userService = userService;

        public async Task<IResponse> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            return await _userService.DeleteAsync(request.UserId);
        }
    }
}

