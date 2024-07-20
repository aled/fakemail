namespace Fakemail.Services
{
    public class CleanupServiceOptions
    {
        public int PollSecondsMin { get; set; }
        public int PollSecondsMax { get; set; }
        public int MaxEmailAgeSeconds { get; set; }
        public int MaxEmailCount { get; set; }
    }
}
