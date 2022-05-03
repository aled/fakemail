namespace Fakemail.ApiModels
{
    public class CreateUserResult
    {
        public bool Success { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
    }
}
