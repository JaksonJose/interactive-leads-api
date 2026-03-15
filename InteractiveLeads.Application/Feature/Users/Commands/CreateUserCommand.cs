using Application.Features.Identity.Users;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Users.Commands
{
    public class CreateUserCommand : IRequest<IResponse>, IValidate
    {
        public CreateUserRequest CreateUser { get; set; }
    }

    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, IResponse>
    {
        private readonly IUserService _userService;

        public CreateUserCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<IResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            return await _userService.CreateAsync(request.CreateUser);
        }
    }
}
