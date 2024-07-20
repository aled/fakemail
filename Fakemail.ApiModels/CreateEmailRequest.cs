using System;

namespace Fakemail.ApiModels
{
    public class CreateEmailRequest : IUserRequest
    {
        public Guid UserId { get; set; }
        public byte[] MimeMessage { get; set; }
    }
}
