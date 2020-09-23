using System;

namespace Fakemail.Models
{
    public record MessageSummary
    {
        public string Id { get; init; }
        public DateTime ReceivedTimestamp { get; init; }
        public string From { get; init; }
        public string Subject { get; init; }
        public string Body { get; init; }
    }
}
