using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;

using Fakemail.ApiModels;
using Fakemail.Web.Models;

using Microsoft.AspNetCore.Mvc;

using MimeKit;

namespace Fakemail.Web.Controllers
{

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IFakemailApi _fakemailApi;

        public HomeController(ILogger<HomeController> logger, IFakemailApi fakemailApi)
        {
            _logger = logger;
            _fakemailApi = fakemailApi;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("user")]
        public async Task<IActionResult> UserCreate()
        {
            var resp = await _fakemailApi.CreateUserAsync(new CreateUserRequest());
            return Redirect($"/user/{resp.UserId}");
        }

        [HttpGet]
        [Route("user/{userId}/update/{sequenceNumber}")]
        public async Task<IActionResult> UserUpdate(Guid userId, int sequenceNumber)
        {
            return PartialView("_ReceivedEmailRows", await GetUserModel(userId, sequenceNumber));
        }

        [HttpGet]
        [Route("user/{userId}")]
        public async Task<IActionResult> UserHome(Guid userId)
        {
            return View("User", await GetUserModel(userId, -1));
        }

        [HttpGet]
        [Route("user/{userId}/smtpuser/{smtpUsername}/inject-test")]
        public async Task<IActionResult> UserInjectTestEmail(Guid userId, string smtpUsername)
        {
            var receivedTimestamp = DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss +0000 (UTC)");
            var dateTimestamp = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm:ss zzz");

            var raw = "Return-Path: <From@From.example.com>\n" +
               "Delivered-To: To@example.stream\n" +
               "Received: from examplehost (static-123-234-12-23.example.co.uk [123.234.12.23])" +
                   $"\tby fakemail.stream (OpenSMTPD) with ESMTPSA id 22e6eb31 (TLSv1.2:ECDHE-RSA-AES256-GCM-SHA384:256:NO) auth=yes user={smtpUsername};" +
                   $"\t{receivedTimestamp}\n" +
               "MIME-Version: 1.0\n" +
               "From: From@From.example.com\n" +
               "To: To@example.stream, To@example2.stream\n" +
               $"Date: {dateTimestamp}\n" +
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

            var response = await _fakemailApi.CreateEmailAsync(new CreateEmailRequest
            {
                UserId = userId,
                MimeMessage = Encoding.UTF8.GetBytes(raw)
            });

            if (response.Success)
            {
                return Ok();
            }

            throw new Exception("Failed to inject test email");
        }

        [HttpGet]
        [Route("user/{userId}/email/{emailId}")]
        public async Task<IActionResult> UserEmailGet(Guid userId, Guid emailId)
        {
            var resp = await _fakemailApi.GetEmailAsync(new GetEmailRequest {
                UserId = userId,
                EmailId = emailId
            });

            if (resp.Success)
            {
                return File(resp.Bytes, MediaTypeNames.Application.Octet, $"{emailId}.eml");
            }

            throw new Exception("Failed to get email");
        }

        [HttpGet]
        [Route("user/{userId}/email/{emailId}/delete")]
        public async Task<IActionResult> UserEmailDelete(Guid userId, Guid emailId)
        {
            var resp = await _fakemailApi.DeleteEmailAsync(new DeleteEmailRequest
            {
                UserId = userId,
                EmailId = emailId
            });

            if (resp.Success)
            {
                return Ok();
            }

            throw new Exception("Failed to delete email");
        }

        private async Task<UserModel> GetUserModel(Guid userId, int sequenceNumber)
        {
            var resp = await _fakemailApi.ListEmailsAsync(new ListEmailsRequest { UserId = userId, Page = 1, PageSize = 100 });

            if (resp.Success)
            {
                return new UserModel()
                {
                    UserId = userId,
                    Username = resp.Username,
                    SmtpCredentials = resp.SmtpUsers.Select(u => new SmtpCredentialModel
                    {
                        SmtpUsername = u.SmtpUsername,
                        SmtpPassword = u.SmtpPassword,
                        EmailCount = u.CurrentEmailCount
                    }).ToList(),
                    EmailAggregation = EmailAggregationModel.Received,
                    EmailSummaries = resp.Emails
                        .Where(x => x.SequenceNumber > sequenceNumber)
                        .Select(x => (EmailSummaryModel)new ReceivedEmailSummaryModel
                        {
                            EmailId = x.EmailId,
                            SequenceNumber = x.SequenceNumber,
                            TimestampUtc = x.TimestampUtc,
                            From = x.From,
                            DeliveredTo = x.DeliveredTo,
                            Subject = x.Subject,
                            Body = x.BodySummary,
                            Attachments = x.Attachments.Select(a => new AttachmentModel
                            {
                                AttachmentId = a.AttachmentId,
                                Name = a.Name
                            }).ToList()
                        }).ToList()
                };
            }

            throw new Exception("Error retrieving user model");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}