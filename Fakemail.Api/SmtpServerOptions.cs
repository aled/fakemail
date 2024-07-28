namespace Fakemail.Api
{
    public class SmtpServerOptions
    {
        public string Host { get; set; }

        public int Port { get; set; }

        public string AuthenticationType { get; set; }
    }
}
