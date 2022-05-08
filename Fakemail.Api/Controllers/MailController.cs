using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Fakemail.ApiModels;
using Fakemail.Core;

namespace Fakemail.Api.Controllers
{
    [Route("api/mail")]
    [Authorize]
    [ApiController]
    public class MailController : ControllerBase
    {
        private readonly IEngine _engine;

        public MailController(IEngine engine)
        {
            _engine = engine;
        }

        // POST: api/mail/list
        [HttpPost]
        [Route("list")]
        public async Task<IActionResult> ListEmail([FromBody] ListEmailRequest request)
        {
            var authenticatedUsername = HttpContext.User.Identity.Name;

            if (!string.IsNullOrEmpty(authenticatedUsername))
            {
                var response = await _engine.ListEmailsAsync(authenticatedUsername, request);
                
                return Ok(response);
            }

            return Unauthorized();
        }
    }
}
