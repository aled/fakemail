namespace Fakemail.Services
{
    public class DeliveryAgentConfig
    {
        public string IncomingDirectory { get; set; } = "/home/fakemail/mail/new";
        public string FailedDirectory { get; set; } = "/home/fakemail/mail/cur";
        public int PollSeconds { get; set; } = 30;
    }
}
