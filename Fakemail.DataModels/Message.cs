using System;

namespace Fakemail.Models
{
    public record Message
    {
        public string Id { get; init; }
        public DateTime ReceivedTimestamp { get; init; }
        public ReadOnlyMemory<byte> Content { get; init; }
    }
}
