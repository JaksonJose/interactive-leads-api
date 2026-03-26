using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;

namespace InteractiveLeads.Application.Feature.Users.Queries
{
    public class GetUserByIdQuery : IApplicationRequest<IResponse>, IValidate
    {
        public Guid UserId { get; set; }
    }

    public class GetUserByIdQueryHandler : IApplicationRequestHandler<GetUserByIdQuery, IResponse>
    {
        private readonly IUserService _userService;

        public GetUserByIdQueryHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<IResponse> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            return await _userService.GetByIdAsync(request.UserId, cancellationToken);
        }
    }
}

