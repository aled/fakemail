namespace Fakemail.Services
{
    public class DeliveryAgentConfig
    {
        public string IncomingDirectory { get; set; } = "c:\\temp\\fakemail\\incoming";
        public string FailedDirectory { get; set; } = "c:\\temp\\fakemail\\failed";
        public int PollSeconds { get; set; } = 30;
    }
}
