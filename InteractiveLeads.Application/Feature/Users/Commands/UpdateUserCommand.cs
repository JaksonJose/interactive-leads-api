using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Users.Commands
{
    public class UpdateUserCommand : IRequest<IResponse>, IValidate
    {
        public UpdateUserRequest UpdateUser { get; set; }
    }

    public class UpdateUserCommandHanlder(IUserService userService) : IRequestHandler<UpdateUserCommand, IResponse>
    {
        private readonly IUserService _userService = userService;

        public async Task<IResponse> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            return await _userService.UpdateAsync(request.UpdateUser);
        }
    }
}
