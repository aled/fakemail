using System;

namespace Fakemail.ApiModels
{
    public class GetEmailRequest : IUserRequest
    {
        public Guid UserId { get; set; }

        public Guid EmailId { get; set; }
    }
}
