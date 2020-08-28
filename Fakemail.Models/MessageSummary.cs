using System;

using Newtonsoft.Json;

namespace Fakemail.Models
{
    public class MessageSummary
    {
        public string Id { get; set; }
        public DateTime ReceivedTimestamp { get; set; }
        public string From { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }
}
