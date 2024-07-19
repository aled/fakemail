namespace Fakemail.ApiModels
{
    public class CleanupEmailsRequest
    {
        public int MaxEmailCount { get; set; }

        public int MaxEmailAgeSeconds { get; set; }
    }
}
