using Fakemail.ApiModels;

namespace Fakemail.Web.Models
{
    public class UserModel
    {
        public Guid UserId { get; set; }

        public string Username { get; set; }

        public SmtpServerModel SmtpServer { get; set; }

        public string ApiExternalBaseUri { get; set; }

        public EmailAggregationModel EmailAggregation { get; set; }

        public List<SmtpCredentialModel> SmtpCredentials { get; set; }

        public List<EmailSummaryModel> EmailSummaries { get; set; }
    }
}
