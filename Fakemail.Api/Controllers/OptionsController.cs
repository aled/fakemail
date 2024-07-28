using Fakemail.ApiModels;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Fakemail.Api.Controllers
{
    [Route("api/options")]
    [ApiController]
    public class OptionsController(IOptions<SmtpServerOptions> smtpServerOptions) : ControllerBase
    {
        // POST: api/options/smtp-server
        [HttpPost]
        [Route("smtpserver")]
        public IActionResult GetSmtpServer([FromBody] GetSmtpServerRequest _)
        {
            var response = new GetSmtpServerResponse
            {
                IsSuccess = true,
                SmtpServer = new SmtpServer
                {
                    Host = smtpServerOptions.Value.Host,
                    Port = smtpServerOptions.Value.Port,
                    AuthenticationType = smtpServerOptions.Value.AuthenticationType
                }
            };

            return Ok(response);
        }
    }
}
