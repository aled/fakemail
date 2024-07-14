using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Net.Mail;
using System.Net;

using Xunit;

namespace Fakemail.IntegrationTests
{
    public class SmtpTests
    {
        private readonly int _port = 587;

        [Fact]
        public async Task UnencryptedSmtpShouldBeRejected()
        {
            var smtpClient = new SmtpClient("fakemail.stream", _port)
            {
                Credentials = new NetworkCredential
                {
                    UserName = "user",
                    Password = "password"
                },
                EnableSsl = false
            };

            var email = new MailMessage
            {
                Subject = "Subject",
                Body = "Body",
                From = new MailAddress("From@From")
            };
            email.To.Add(new MailAddress("To@To"));

            var send = new Func<Task>(() => smtpClient.SendMailAsync(email));
            var exception = await Assert.ThrowsAsync<SmtpException>(send);
            Assert.Equal(SmtpStatusCode.MustIssueStartTlsFirst, exception.StatusCode);
        }

        [Fact]
        public async Task SendEmailWithSsl()
        {
            // Uncomment following line to accept invalid server certificate
            //ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            var smtpClient = new SmtpClient("fakemail.stream", _port)
            {
                Credentials = new NetworkCredential
                {
                    UserName = "1tpxdz",
                    Password = "9o77B2C7gEU"
                },
                EnableSsl = true
            };

            var email = new MailMessage
            {
                Subject = "Subject",
                Body = "Body",
                From = new MailAddress("From@From.example.com")
            };
            email.To.Add(new MailAddress("To@example1.stream"));           
            email.CC.Add(new MailAddress("To@example2.stream"));
            email.CC.Add(new MailAddress("To@example3.stream"));
            email.Bcc.Add(new MailAddress("To@example3.stream"));
            email.Bcc.Add(new MailAddress("To@example4.stream"));
            var content = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
            var attachment = new Attachment(content, "a.txt");
            
            email.Attachments.Add(attachment);
            await smtpClient.SendMailAsync(email);
        }
    }
}
