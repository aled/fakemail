using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fakemail.Data.EntityFramework
{
    /// <summary>
    /// This represents an email that has been delivered to a mailbox.
    ///
    /// This is different to an email that has been sent, as there is one delivery
    /// for each unique To, CC, or BCC address.
    ///
    /// To obtain the sent email, need to aggregate delivered emails by all fields
    /// except DeliveredTo (BCC is all recipients not included in To or CC)
    /// </summary>
    public class Email
    {
        [Required]
        [Key]
        public Guid EmailId { get; set; }

        /// <summary>
        /// Sequence number for email - per SmtpUser
        /// </summary>
        [Required]
        public int SequenceNumber { get; set; }

        /// <summary>
        /// The raw mime-encoded message.
        /// </summary>
        [Required]
        public byte[] MimeMessage { get; set; }

        // Common headers, displayed on summary screen
        [Required]
        public string From { get; set; }

        [Required]
        public string To { get; set; }

        [Required]
        public string CC { get; set; }

        [Required]
        public string DeliveredTo { get; set; }

        [Required]
        public string Subject { get; set; }

        // Parse the content of the Received header into more granular properties
        [Required]
        public string ReceivedFromHost { get; set; }

        [Required]
        public string ReceivedFromDns { get; set; }

        [Required]
        public string ReceivedFromIp { get; set; }

        [Required]
        public string ReceivedSmtpId { get; set; }

        [Required]
        public string ReceivedTlsInfo { get; set; }

        [Required]
        public DateTime ReceivedTimestampUtc { get; set; }

        /// <summary>
        /// Number of bytes in body content
        /// </summary>
        [Required]
        public int BodyLength { get; set; }

        /// <summary>
        /// Contains first 250 chars of body
        /// </summary>
        [Required]
        public string BodySummary { get; set; }

        [Required]
        public int BodyChecksum { get; set; }

        public List<Attachment> Attachments { get; set; }
        public string SmtpUsername { get; set; }
        public SmtpUser SmtpUser { get; set; }
    }
}
