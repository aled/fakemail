using System.Diagnostics;
using System.Text.Json;

using Fakemail.ApiModels;
using Fakemail.Web.Models;

using Microsoft.AspNetCore.Mvc;

namespace Fakemail.Web.Controllers
{
    static class HttpExtensions
    {
        public static async Task<T> FromJsonAsync<T>(this HttpContent content)
        {
            var json = await content.ReadAsStringAsync() ?? throw new Exception();    
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new Exception();
        }
    }

    public class FakemailApi
    {
        private async Task<TResp> CallAsync<TReq, TResp>(TReq req, string path)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.PostAsJsonAsync($"http://localhost:5053/api/{path}", req);
            return await response.Content.FromJsonAsync<TResp>();
        }

        public Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request) =>
            CallAsync<CreateUserRequest, CreateUserResponse>(request, "user/create");

        public Task<ListEmailsResponse> ListEmailsAsync(ListEmailsRequest request) =>
            CallAsync<ListEmailsRequest, ListEmailsResponse>(request, "mail/list");        
    }

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("user")]
        public async Task<IActionResult> UserCreate()
        {
            var resp = await new FakemailApi().CreateUserAsync(new CreateUserRequest());
            return Redirect($"/user/{resp.UserId}");
        }

        [HttpGet]
        [Route("user/{userId}")]
        public async Task<IActionResult> UserHome(Guid userId)
        {           
            var resp = await new FakemailApi().ListEmailsAsync(new ListEmailsRequest { UserId = userId });

            if (resp.Success)
            {
                var userModel = new UserModel()
                {
                    Username = resp.Username,
                    SmtpCredentials = resp.SmtpUsers.Select(u => new SmtpCredentialModel 
                    { 
                        SmtpUsername = u.SmtpUsername, 
                        SmtpPassword = u.SmtpPassword, 
                        EmailCount = u.CurrentEmailCount 
                    }).ToList(),
                    EmailAggregation = EmailAggregationModel.Received,
                    EmailSummaries = resp.Emails.Select(x => (EmailSummaryModel) new ReceivedEmailSummaryModel
                    {
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

                return View("User", userModel);
            }

            throw new Exception(resp.ErrorMessage);
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