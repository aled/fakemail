namespace Fakemail.Web.Models
{
    public class SmtpServerModel
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string AuthenticationType { get; set; }
    }
}
