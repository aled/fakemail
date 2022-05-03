using System;

using System.ComponentModel.DataAnnotations;

namespace Fakemail.Data.EntityFramework
{
    public class Attachment
    {
        [Required]
        [Key]
        public Guid AttachmentId { get; set; }

        [Required]
        public Guid EmailId { get; set; }

        [Required]
        public string Filename { get; set; }

        [Required]
        public string ContentType { get; set; }

        [Required]
        public byte[] Content { get; set; }

        /// <summary>
        /// Checksum of the content
        /// </summary>
        [Required]
        public int ContentChecksum { get; set; }

        public Email Email { get; set; }
    }
}
