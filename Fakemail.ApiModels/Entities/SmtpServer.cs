namespace Fakemail.ApiModels
{
    public class SmtpServer
    {
        public string Host { get; set; }

        public int Port { get; set; }

        public string AuthenticationType { get; set; }
    }
}
