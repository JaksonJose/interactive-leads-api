using InteractiveLeads.Application.Feature.Users;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Application.Feature.CrossTenant.Commands
{
    /// <summary>
    /// Command for creating a new global Support user (TenantId = null). SysAdmin only.
    /// </summary>
    public sealed class CreateSupportUserCommand : IRequest<IResponse>, IValidate
    {
        public CreateUserRequest CreateUser { get; set; } = new();
    }

    /// <summary>
    /// Handler for CreateSupportUserCommand. Creates a user with TenantId = null and role Support.
    /// </summary>
    public sealed class CreateSupportUserCommandHandler : IRequestHandler<CreateSupportUserCommand, IResponse>
    {
        private readonly IUserService _userService;

        public CreateSupportUserCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<IResponse> Handle(CreateSupportUserCommand request, CancellationToken cancellationToken)
        {
            return await _userService.CreateSupportUserAsync(request.CreateUser);
        }
    }
}
