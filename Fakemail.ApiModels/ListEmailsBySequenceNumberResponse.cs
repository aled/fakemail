using System.Collections.Generic;

namespace Fakemail.ApiModels
{
    public class ListEmailsBySequenceNumberResponse
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string Username { get; set; }
        public List<EmailSummary> Emails { get; set; }
    }
}
