using System;

namespace Fakemail.ApiModels
{
    public class DeleteAllEmailsRequest : IUserRequest
    {
        public Guid UserId { get; set; }

        public string SmtpUsername { get; set; }
    }
}
