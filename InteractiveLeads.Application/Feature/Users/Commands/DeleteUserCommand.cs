using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Users.Commands
{
    public class DeleteUserCommand : IRequest<IResponse>, IValidate
    {
        public Guid UserId { get; set; }
    }

    public class DeleteUserCommandHandler(IUserService userService)
        : IRequestHandler<DeleteUserCommand, IResponse>
    {
        private readonly IUserService _userService = userService;

        public async Task<IResponse> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            return await _userService.DeleteAsync(request.UserId);
        }
    }
}
