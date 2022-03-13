using System;

namespace Fakemail.ApiModels
{
    public class Email
    {
        public Guid EmailId { get; set; }
        public DateTime ReceivedTimestamp { get; set; }
        public string From { get; set; }
        public string[] To { get; set; }
        public string Subject { get; set; }
        public string TextBody { get; set; }
        public Attachment[] Attachments { get; set; }
    }
}
