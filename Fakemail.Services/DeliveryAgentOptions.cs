namespace Fakemail.Services
{
    public class DeliveryAgentOptions
    {
        public string IncomingDirectory { get; set; }
        public string FailedDirectory { get; set; }
        public int PollSeconds { get; set; }
    }
}
