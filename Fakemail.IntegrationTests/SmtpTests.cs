using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Linq;
using System.Net.Mail;
using System.Net;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Xunit;

using Serilog;

using SmtpServer.Authentication;
using SmtpServer.Storage;

using Fakemail.Core;
using Fakemail.Data;
using Fakemail.Data.EntityFramework;


namespace Fakemail.IntegrationTests
{
   

    public class SmtpTests
    {
        private int _port = 465;

        //public SmtpTests(SmtpFixture smtpFixture)
        //{
        //    _port = smtpFixture.SmtpService.Ports.First();
        //}

        [Fact]
        public async Task SendEmail()
        {
            

            var smtpClient = new SmtpClient("fakemail.stream", _port);
            smtpClient.Credentials = new NetworkCredential
            {
                UserName = "user",
                Password = "password"
            };
            smtpClient.EnableSsl = false;

            var email = new MailMessage();
            email.Subject = "Subject";
            email.Body = "Body";
            email.From = new MailAddress("From@From");
            email.To.Add(new MailAddress("To@To"));

            await smtpClient.SendMailAsync(email);
        }

        [Fact]
        public async Task SendEmailWithSsl()
        {
            // Uncomment following line to accept invalid server certificate
            //ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            var smtpClient = new SmtpClient("fakemail.stream", _port);
            smtpClient.Credentials = new NetworkCredential
            {
                UserName = "user2",
                Password = "Hello world!"
            };
            smtpClient.EnableSsl = true;

            var email = new MailMessage();
            email.Subject = "Subject";
            email.Body = "Body";
            email.From = new MailAddress("From@From.example.com");
            email.To.Add(new MailAddress("To@example.stream"));
            email.To.Add(new MailAddress("To@example2.stream"));
            var content = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
            var attachment = new Attachment(content, "a.txt");
            
            email.Attachments.Add(attachment);
            await smtpClient.SendMailAsync(email);
        }
    }
}
