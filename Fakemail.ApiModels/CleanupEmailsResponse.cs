namespace Fakemail.ApiModels
{
    public class CleanupEmailsResponse
    {
        
    
        public bool Success { get; set; } = false;

        public string ErrorMessage { get; set; } = null;

        public int TotalEmailsDeleted { get; set; }
    }
}
