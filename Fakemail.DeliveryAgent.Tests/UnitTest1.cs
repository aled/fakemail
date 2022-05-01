using System;
using System.IO;
using System.Text;
using System.Linq;

using Xunit;
using MimeKit;

namespace Fakemail.DeliveryAgent.Tests
{
    static class Extensions
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

    public class UnitTest1
    {
        [Fact]
        public void ShouldParseMimeEncodedMessage()
        {
            var raw = "Return-Path: <From@From.example.com>\n" +
                "Delivered-To: To@example2.stream\n" +
                "Received: from examplehost (static-123-234-12-23.example.co.uk [123.234.12.23])\n" +
                "        by fakemail.stream (OpenSMTPD) with ESMTPSA id 392ecef5 (TLSv1.2:ECDHE-RSA-AES256-GCM-SHA384:256:NO) auth=yes user=user234;\n" +
                "        Fri, 29 Apr 2022 21:28:41 +0000 (UTC)\n" +
                "MIME-Version: 1.0\n" +
                "From: From@From.example.com\n" +
                "To: To@example.stream, To@example2.stream\n" +
                "Date: 29 Apr 2022 22:28:42 +0100\n" +
                "Subject: Subject\n" +
                "Content-Type: text/plain; charset=us-ascii\n" +
                "Content-Transfer-Encoding: quoted-printable\n" +
                "\n" +
                "Body\n" +
                "\n";

            var stream = new MemoryStream(Encoding.ASCII.GetBytes(raw));

            var message = MimeMessage.Load(stream);

            Assert.NotNull(message);

            // Check the message headers
            Assert.Equal("<From@From.example.com>", message.Headers["Return-Path"]);
            Assert.Equal("To@example2.stream", message.Headers["Delivered-To"]);
            Assert.Equal("from examplehost (static-123-234-12-23.example.co.uk [123.234.12.23])" +
                "        by fakemail.stream (OpenSMTPD) with ESMTPSA id 392ecef5 (TLSv1.2:ECDHE-RSA-AES256-GCM-SHA384:256:NO) auth=yes user=user234;" +
                "        Fri, 29 Apr 2022 21:28:41 +0000 (UTC)", message.Headers["Received"]);
            Assert.Equal("1.0", message.Headers["MIME-Version"]);
            Assert.Equal("29 Apr 2022 22:28:42 +0100", message.Headers["Date"]);

            // These are part of the body, rather than the message headers
            Assert.Null(message.Headers["Content-Type"]);
            Assert.Null(message.Headers["Content-Transfer-Encoding"]);

            Assert.Equal(new InternetAddressList(new[] { InternetAddress.Parse("From@From.example.com") }), message.From);
            Assert.Equal(new InternetAddressList(new[] { InternetAddress.Parse("To@example.stream"), InternetAddress.Parse("To@example2.stream") }), message.To);

            Assert.Equal("text/plain", message.Body.ContentType.MimeType);

            // Not sure this is correct. The sent body did not have any \r or \n characters, but the received one does, hence needs the Trim() to pass
            Assert.Equal("Body", message.TextBody.Trim());

            // Create the data model

        }

        [Fact]
        public void ShouldParseMimeEncodedMessageWithAttachment()
        {
            var raw = "Return-Path: <From@From.example.com>\n" +
                "Delivered-To: To@example.stream\n" +
                "Received: from examplehost (static-123-234-12-23.example.co.uk [123.234.12.23])\n" +
                "        by fakemail.stream (OpenSMTPD) with ESMTPSA id 22e6eb31 (TLSv1.2:ECDHE-RSA-AES256-GCM-SHA384:256:NO) auth=yes user=user345;\n" +
                "        Sat, 30 Apr 2022 13:43:22 +0000 (UTC)\n" +
                "MIME-Version: 1.0\n" +
                "From: From@From.example.com\n" +
                "To: To@example.stream, To@example2.stream\n" +
                "Date: 30 Apr 2022 14:43:23 +0100\n" +
                "Subject: Subject\n" +
                "Content-Type: multipart/mixed;\n" +
                " boundary=--boundary_0_49d7d9ea-01d1-4f5c-91a5-19930730ea52\n" +
                "\n" +
                "\n" +
                "----boundary_0_49d7d9ea-01d1-4f5c-91a5-19930730ea52\n" +
                "Content-Type: text/plain; charset=us-ascii\n" +
                "Content-Transfer-Encoding: quoted-printable\n" +
                "\n" +
                "Body\n" +
                "----boundary_0_49d7d9ea-01d1-4f5c-91a5-19930730ea52\n" +
                "Content-Type: application/octet-stream; name=a.txt\n" +
                "Content-Transfer-Encoding: base64\n" +
                "Content-Disposition: attachment\n" +
                "\n" +
                "aGVsbG8=\n" +
                "----boundary_0_49d7d9ea-01d1-4f5c-91a5-19930730ea52--\n" +
                "\n";

            var stream = new MemoryStream(Encoding.ASCII.GetBytes(raw));

            var message = MimeMessage.Load(stream);

            Assert.NotNull(message);

            // Check the message headers
            Assert.Equal("<From@From.example.com>", message.Headers["Return-Path"]);
            Assert.Equal("To@example.stream", message.Headers["Delivered-To"]);
            Assert.Equal("from examplehost (static-123-234-12-23.example.co.uk [123.234.12.23])" +
                "        by fakemail.stream (OpenSMTPD) with ESMTPSA id 22e6eb31 (TLSv1.2:ECDHE-RSA-AES256-GCM-SHA384:256:NO) auth=yes user=user345;" +
                "        Sat, 30 Apr 2022 13:43:22 +0000 (UTC)", message.Headers["Received"]);
            Assert.Equal("1.0", message.Headers["MIME-Version"]);
            Assert.Equal("30 Apr 2022 14:43:23 +0100", message.Headers["Date"]);

            // These are part of the body, rather than the message headers
            Assert.Null(message.Headers["Content-Type"]);
            Assert.Null(message.Headers["Content-Transfer-Encoding"]);

            Assert.Equal(new InternetAddressList(new[] { InternetAddress.Parse("From@From.example.com") }), message.From);
            Assert.Equal(new InternetAddressList(new[] { InternetAddress.Parse("To@example.stream"), InternetAddress.Parse("To@example2.stream") }), message.To);

            Assert.Equal("multipart/mixed", message.Body.ContentType.MimeType);

            Assert.Equal("Body", message.TextBody);

            Assert.Single(message.Attachments);
            Assert.Equal("a.txt", message.Attachments.First().ContentType.Name);
            Assert.Equal(Encoding.UTF8.GetBytes("hello"), message.Attachments.First().GetContentBytes());
        }
    }
}