namespace Fakemail.ApiModels
{
    public class CreateUserRequest
    {
        /// <summary>
        /// Optional
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Optional. User will be unsecured.
        /// </summary>
        public string Password { get; set; }
    }
}
