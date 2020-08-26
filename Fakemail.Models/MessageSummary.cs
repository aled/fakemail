using System;

namespace Fakemail.Models
{
    public class MessageSummary
    {
        public string Id { get; }
        public DateTime ReceivedTimestamp { get; }
        public string From { get; }
        public string Subject { get; }
        public string Body { get; }

        public MessageSummary(string id, DateTime receivedTimestamp, string from, string subject, string body)
        {
            Id = id;
            ReceivedTimestamp = receivedTimestamp;
            From = from;
            Subject = subject;
            Body = body;
        }
    }
}
