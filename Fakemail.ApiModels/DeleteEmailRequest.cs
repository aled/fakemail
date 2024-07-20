using System;

namespace Fakemail.ApiModels
{
    public class DeleteEmailRequest : IUserRequest
    {
        public Guid UserId { get; set; }

        public Guid EmailId { get; set; }
    }
}
