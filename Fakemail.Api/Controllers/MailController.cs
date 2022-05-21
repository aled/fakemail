using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Fakemail.ApiModels;
using Fakemail.Core;

namespace Fakemail.Api.Controllers
{
    [Route("api/mail")]
    [ApiController]
    public class MailController : ControllerBase
    {
        private readonly IEngine _engine;

        public MailController(IEngine engine)
        {
            _engine = engine;
        }

        // POST: api/mail/get
        [HttpPost]
        [Route("get")]
        public async Task<IActionResult> GetEmail([FromBody] GetEmailRequest request)
        {
            Guid.TryParse(HttpContext.User.Identity.Name, out var authenticatedUserId);
            var response = await _engine.GetEmailAsync(request, authenticatedUserId);
            return Ok(response);
        }

        // POST: api/mail/delete
        [HttpPost]
        [Route("delete")]
        public async Task<IActionResult> DeleteEmail([FromBody] DeleteEmailRequest request)
        {
            Guid.TryParse(HttpContext.User.Identity.Name, out var authenticatedUserId);
            var response = await _engine.DeleteEmailAsync(request, authenticatedUserId);
            return Ok(response);
        }

        // POST: api/mail/list
        [HttpPost]
        [Route("list")]
        public async Task<IActionResult> ListEmails([FromBody] ListEmailsRequest request)
        {
            Guid.TryParse(HttpContext.User.Identity.Name, out var authenticatedUserId);            
            var response = await _engine.ListEmailsAsync(request, authenticatedUserId);               
            return Ok(response);
        }

        // POST: api/mail/create
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateEmail([FromBody] CreateEmailRequest request)
        {
            Guid.TryParse(HttpContext.User.Identity.Name, out var authenticatedUserId);
            var response = await _engine.CreateEmailAsync(request, authenticatedUserId);
            return Ok(response);
        }
    }
}
