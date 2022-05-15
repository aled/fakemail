namespace Fakemail.Web.Models
{
    public class SmtpCredentialModel
    {
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public int EmailCount { get; set; }
    }

    public class AttachmentModel
    {
        public Guid AttachmentId { get; set; }
        public string Name { get; set; }
    }

    public class EmailSummaryModel
    {
        public Guid EmailId { get; set; }
        public int SequenceNumber { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string From { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public List<AttachmentModel> Attachments { get; set; }
    }

    public class SentEmailSummaryModel : EmailSummaryModel
    {
        public List<string> To { get; set; }
        public List<string> CC { get; set; }
        public List<string> BCC { get; set; }
    }

    public class ReceivedEmailSummaryModel : EmailSummaryModel
    {
        public Guid EnvelopeId { get; set; }
        public string DeliveredTo { get; set; }
    }

    public enum EmailAggregationModel
    {
        Received,
        Sent
    }

    public class UserModel
    {
        public string Username { get; set; }

        public EmailAggregationModel EmailAggregation { get; set; }

        public List<SmtpCredentialModel> SmtpCredentials { get; set; }

        public List<EmailSummaryModel> EmailSummaries { get; set; }
    }
}
