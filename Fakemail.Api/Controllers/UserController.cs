using System.Security.Claims;

using Fakemail.ApiModels;
using Fakemail.Core;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fakemail.Api.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IJwtAuthentication _auth;
        private readonly IEngine _engine;

        public UserController(IJwtAuthentication auth, IEngine engine)
        {
            _auth = auth;
            _engine = engine;
        }

        // GET: api/user/all
        [Authorize(Roles="admin")]
        [HttpGet]
        [Route("all")]
        public async Task<IActionResult> ListUsers(int page, int pageSize)
        {
            var request = new ListUserRequest { Page = page, PageSize = pageSize }; 
            var response = await _engine.ListUsers(request);

            return Ok(response);
        }

        // GET api/<userController>/5
        //[Authorize(Roles = "Admin")]
        //[HttpGet("{id}")]
        //public user Get(int id)
        //{
        //    return _users.Find(x => x.Id == id);
        //}

        // POST api/user/create
        [AllowAnonymous]
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> PostAsync([FromBody] CreateUserRequest request)
        {
            var response = await _engine.CreateUserAsync(request);

            return Ok(response);            
        }

        // POST api/user/token
        [AllowAnonymous]
        [HttpPost]
        [Route("token")]
        public async Task<IActionResult> PostAsync([FromBody] GetTokenRequest request)
        {
            var response = await _engine.GetTokenAsync(request);

            if (response.Success)
            {
                return Ok(response);
            }

            return Unauthorized();
        }
      
        // DELETE api/<userController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
