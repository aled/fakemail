namespace Fakemail.ApiModels
{
    public class DeleteEmailResponse
    {
        public bool Success { get; set; } = false;

        public string ErrorMessage { get; set; } = null;
    }
}
