using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Users.Queries
{
    public class GetUserByIdQuery : IRequest<IResponse>, IValidate
    {
        public Guid UserId { get; set; }
    }

    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, IResponse>
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
