using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Fakemail.ApiModels;
using Fakemail.Core;

namespace Fakemail.Api.Controllers
{
    [Route("api/mail")]
    [ApiController]
    public class MailController(IEngine engine) : ControllerBase
    {
        // POST: api/mail/get
        [HttpPost]
        [Route("get")]
        public async Task<IActionResult> GetEmail([FromBody] GetEmailRequest request)
        {
            _ = Guid.TryParse(HttpContext.User.Identity.Name, out var authenticatedUserId);
            var response = await engine.GetEmailAsync(request, authenticatedUserId);
            return Ok(response);
        }

        // POST: api/mail/delete
        [HttpPost]
        [Route("delete")]
        public async Task<IActionResult> DeleteEmail([FromBody] DeleteEmailRequest request)
        {
            _ = Guid.TryParse(HttpContext.User.Identity.Name, out var authenticatedUserId);
            var response = await engine.DeleteEmailAsync(request, authenticatedUserId);
            return Ok(response);
        }

        // POST: api/mail/list
        [HttpPost]
        [Route("list")]
        public async Task<IActionResult> ListEmails([FromBody] ListEmailsRequest request)
        {
            _ = Guid.TryParse(HttpContext.User.Identity.Name, out var authenticatedUserId);
            var response = await engine.ListEmailsAsync(request, authenticatedUserId);
            return Ok(response);
        }

        // POST: api/mail/create
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateEmail([FromBody] CreateEmailRequest request)
        {
            _ = Guid.TryParse(HttpContext.User.Identity.Name, out var authenticatedUserId);
            var response = await engine.CreateEmailAsync(request, authenticatedUserId);
            return Ok(response);
        }
    }
}
