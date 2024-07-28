using Fakemail.ApiModels;
using Fakemail.Core;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Fakemail.Api.Controllers
{
    [Route("api/mail")]
    [ApiController]
    public class MailController(IEngine engine, IOptions<SmtpServerOptions> smtpServerOptions) : ControllerBase
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

        // POST: api/mail/deleteall
        [HttpPost]
        [Route("delete-all")]
        public async Task<IActionResult> DeleteAllEmails([FromBody] DeleteAllEmailsRequest request)
        {
            _ = Guid.TryParse(HttpContext.User.Identity.Name, out var authenticatedUserId);
            var response = await engine.DeleteAllEmailsAsync(request, authenticatedUserId);
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

        // POST: api/mail/listbysequencenumber
        [HttpPost]
        [Route("list-by-sequence-number")]
        public async Task<IActionResult> ListEmailsBySequenceNumber([FromBody] ListEmailsBySequenceNumberRequest request)
        {
            _ = Guid.TryParse(HttpContext.User.Identity.Name, out var authenticatedUserId);
            var response = await engine.ListEmailsBySequenceNumberAsync(request, authenticatedUserId);
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

        // POST: api/mail/testsmtp
        [HttpPost]
        [Route("test-smtp")]
        public async Task<IActionResult> TestSmtp([FromBody] TestSmtpRequest request)
        {
            _ = Guid.TryParse(HttpContext.User.Identity.Name, out var authenticatedUserId);

            var smtpServer = new SmtpServer
            {
                Host = smtpServerOptions.Value.Host,
                Port = smtpServerOptions.Value.Port,
                AuthenticationType = smtpServerOptions.Value.AuthenticationType
            };

            var response = await engine.TestSmtpAsync(request, authenticatedUserId, smtpServer);
            return Ok(response);
        }
    }
}
