namespace Fakemail.ApiModels
{
    public class GetSmtpServerResponse
    {
        public bool IsSuccess { get; set; }

        public string ErrorMessage { get; set; }

        public SmtpServer SmtpServer { get; set; }
    }
}
