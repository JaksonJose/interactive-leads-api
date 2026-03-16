using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.Activation;
using InteractiveLeads.Application.Feature.Activation.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers
{
    /// <summary>
    /// Account activation (invitation link). No tenant in request; token identifies the user and tenant.
    /// </summary>
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ActivationController : BaseApiController
    {
        /// <summary>
        /// Activates an invited user's account using the token from the invitation link. Sets password and activates the user.
        /// </summary>
        [HttpPost("activate")]
        [AllowAnonymous]
        [OpenApiOperation("Activate account with token from invitation link")]
        public async Task<IActionResult> ActivateAsync([FromBody] ActivateAccountRequest request)
        {
            var response = await Sender.Send(new ActivateAccountCommand
            {
                Token = request.Token,
                NewPassword = request.NewPassword,
                ConfirmPassword = request.ConfirmPassword
            });
            return Ok(response);
        }
    }
}
