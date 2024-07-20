namespace Fakemail.Web.Models
{
    public abstract class EmailSummaryModel
    {
        public Guid EmailId { get; set; }
        public int SequenceNumber { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string From { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public List<AttachmentModel> Attachments { get; set; }
    }
}
