using System;
using System.Collections.Generic;

namespace Fakemail.ApiModels
{
    public class EmailSummary
    {
        public Guid EmailId { get; set; }
        public int SequenceNumber { get; set; }
        public string Username { get; set; }
        public string SmtpUsername { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string From { get; set; }
        public string Subject { get; set; }
        public string DeliveredTo { get; set; }
        public string BodySummary { get; set; }
        public List<AttachmentSummary> Attachments { get; set; }
    }
}
