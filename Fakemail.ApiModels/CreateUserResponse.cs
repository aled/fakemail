namespace Fakemail.ApiModels
{
    public class CreateUserResponse
    {
        public bool Success { get; set; } = false;
        public string ErrorMessage { get; set; } = null;
        public string Username { get; set; } = null;
        
        /// <summary>
        /// If the password was created by the server, return it in the response.
        /// </summary>
        public string Password { get; set; }
        public string SmtpUsername { get; set; } = null;
        public string SmtpPassword { get; set; } = null;

        /// <summary>
        /// Newly-created bearer token to allow the user to call the API immediately
        /// </summary>
        public string BearerToken { get; set; } = null;
    }
}
