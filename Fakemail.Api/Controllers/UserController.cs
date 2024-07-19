using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Fakemail.ApiModels;
using Fakemail.Core;

namespace Fakemail.Api.Controllers
{
    [Route("api/user")]
    [Authorize]
    [ApiController]
    public class UserController(IEngine engine) : ControllerBase
    {

        // POST: api/user/list
        [Authorize(Roles="admin")]
        [HttpPost]
        [Route("list")]
        public async Task<IActionResult> ListUsers([FromBody] ListUserRequest request)
        {
            var response = await engine.ListUsersAsync(request);

            return Ok(response);
        }

        // POST api/user/create
        [AllowAnonymous]
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> PostAsync([FromBody] CreateUserRequest request)
        {
            var response = await engine.CreateUserAsync(request);

            return Ok(response);
        }

        // POST api/user/token
        [AllowAnonymous]
        [HttpPost]
        [Route("token")]
        public async Task<IActionResult> PostAsync([FromBody] GetTokenRequest request)
        {
            var response = await engine.GetTokenAsync(request);

            if (response.Success)
            {
                return Ok(response);
            }

            return Unauthorized();
        }
    }
}
