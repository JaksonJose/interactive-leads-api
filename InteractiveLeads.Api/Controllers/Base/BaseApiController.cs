using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteractiveLeads.Api.Controllers.Base
{
    /// <summary>
    /// Base controller for all API controllers providing common functionality.
    /// </summary>
    /// <remarks>
    /// This controller provides access to MediatR for implementing the CQRS pattern.
    /// All API controllers should inherit from this base controller.
    /// Requires authorization by default.
    /// </remarks>
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class BaseApiController : ControllerBase
    {
        private ISender? _sender = null;

        /// <summary>
        /// Gets the MediatR sender for sending commands and queries.
        /// </summary>
        /// <remarks>
        /// Lazily initializes the sender from the HTTP context request services.
        /// This allows controllers to send commands and queries using the CQRS pattern.
        /// </remarks>
        public ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();
    }
}
