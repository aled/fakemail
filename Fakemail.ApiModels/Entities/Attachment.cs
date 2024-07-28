using System;

namespace Fakemail.ApiModels
{
    public class Attachment
    {
        public Guid AttachmentId { get; set; }
        public string Filename { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
    }
}
