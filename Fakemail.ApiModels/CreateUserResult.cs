namespace Fakemail.ApiModels
{
    public class CreateUserResult
    {
        public bool Success { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }
}
