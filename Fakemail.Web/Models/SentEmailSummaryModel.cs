namespace Fakemail.Web.Models
{
    public class SentEmailSummaryModel : EmailSummaryModel
    {
        public List<string> To { get; set; }
        public List<string> CC { get; set; }
        public List<string> BCC { get; set; }
    }
}
