using System;

namespace Fakemail.DataModels
{
    public class Attachment
    {
        public Guid AttachmentId { get; set; }
        public string Filename { get; set; }
        public byte[] Content { get; set; } 
    }
}
