using System.Collections.Generic;
using System;

namespace Fakemail.ApiModels
{
    public class EmailSummary
    {
        public Guid EmailId { get; set; }
        public string Username { get; set; }
        public string SmtpUsername { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string Subject { get; set; }
        public string DeliveredTo { get; set; }
        public string BodySummary { get; set; }
        public List<AttachmentSummary> Attachments { get; set; }
    }
}
