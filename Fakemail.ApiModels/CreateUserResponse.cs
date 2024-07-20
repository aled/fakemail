using System;

namespace Fakemail.ApiModels
{
    public class CreateUserResponse
    {
        public bool Success { get; set; } = false;

        public string ErrorMessage { get; set; } = null;

        public Guid? UserId { get; set; } = null;

        public string Username { get; set; } = null;

        public string SmtpUsername { get; set; } = null;

        public string SmtpPassword { get; set; } = null;
    }
}
