using System;

namespace Fakemail.ApiModels
{
    public class TestSmtpRequest : IUserRequest
    {
        public Guid UserId { get; set; }

        public string SmtpUsername { get; set; }

        public Email Email { get; set; }
    }
}
