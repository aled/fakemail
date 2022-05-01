using System;
using System.ComponentModel.DataAnnotations;

namespace Fakemail.DataModels
{
    public class Email
    {
        [Required]
        public Guid EmailId { get; set; }
        
        [Required]
        public DateTime ReceivedTimestamp { get; set; }

        [Required]
        public string From { get; set; }

        [Required]
        public string To { get; set; }

        [Required]
        public string Subject { get; set; }

        [Required]
        public string TextBody { get; set; }

        [Required]
        public Attachment[] Attachments { get; set; }

        [Required]
        public Guid UserId { get; set; }
        
        public User User { get; set; }
    }
}
