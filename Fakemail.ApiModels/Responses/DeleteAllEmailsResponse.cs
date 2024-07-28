namespace Fakemail.ApiModels
{
    public class DeleteAllEmailsResponse
    {
        public bool Success { get; set; } = false;

        public string ErrorMessage { get; set; } = null;

        public int EmailDeletedCount { get; set; }
    }
}
