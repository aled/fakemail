using System;

namespace Fakemail.Models
{
    public class Message
    {
        public string Id { get; }
        public DateTime ReceivedTimestamp { get; }
        public byte[] Content { get; }
        
        public Message(string id, DateTime receivedTimestamp, byte[] content)
        {
            Id = id;
            ReceivedTimestamp = receivedTimestamp;
            Content = content;
        }
    }
}
