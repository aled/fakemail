using System.IO;

using MimeKit;

namespace Fakemail.Core
{
    public static class MimeEntityExtensions
    {
        public static byte[] GetContentBytes(this MimeEntity mimeEntity)
        {
            using (var m = new MemoryStream())
            {
                if (mimeEntity is MimePart mimePart)
                {
                    mimePart.Content.DecodeTo(m);
                }
                else if (mimeEntity is MessagePart messagePart)
                {
                    messagePart.Message.WriteTo(m);
                }
                return m.ToArray();
            }
        }
    }
}