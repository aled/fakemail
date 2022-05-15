using System.Diagnostics;

using Fakemail.ApiModels;
using Fakemail.Web.Models;

using Microsoft.AspNetCore.Mvc;

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