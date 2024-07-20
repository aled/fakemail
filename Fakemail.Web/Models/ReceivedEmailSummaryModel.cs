namespace Fakemail.Web.Models
{
    public class ReceivedEmailSummaryModel : EmailSummaryModel
    {
        public Guid EnvelopeId { get; set; }
        public string DeliveredTo { get; set; }
    }
}
