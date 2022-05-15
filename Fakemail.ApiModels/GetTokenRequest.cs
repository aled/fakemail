using System;

namespace Fakemail.ApiModels
{
    public class GetTokenRequest
    {
        public Guid UserId { get; set; }

        public string Password { get; set; }
    }
}
