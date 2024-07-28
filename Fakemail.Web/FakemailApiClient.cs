using Fakemail.ApiModels;

using Microsoft.Extensions.Options;

namespace Fakemail.Web
{
    public class FakemailApiClient(HttpClient httpClient, IOptions<FakemailApiClientOptions> options) : IFakemailApiClient
    {
        public string ExternalBaseUri => options.Value.ExternalBaseUri;

        private async Task<TResp> CallAsync<TReq, TResp>(TReq req, string path)
        {
            var response = await httpClient.PostAsJsonAsync($"{options.Value.BaseUri}{path}", req);
            return await response.Content.FromJsonAsync<TResp>();
        }

        public Task<GetSmtpServerResponse> GetSmtpServerAsync(GetSmtpServerRequest request) =>
            CallAsync<GetSmtpServerRequest, GetSmtpServerResponse>(request, "options/smtpserver");

        public Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request) =>
            CallAsync<CreateUserRequest, CreateUserResponse>(request, "user/create");

        public Task<ListEmailsResponse> ListEmailsAsync(ListEmailsRequest request) =>
            CallAsync<ListEmailsRequest, ListEmailsResponse>(request, "mail/list");

        public Task<ListEmailsBySequenceNumberResponse> ListEmailsBySequenceNumberAsync(ListEmailsBySequenceNumberRequest request) =>
            CallAsync<ListEmailsBySequenceNumberRequest, ListEmailsBySequenceNumberResponse>(request, "mail/list-by-sequence-number");

        public Task<GetEmailResponse> GetEmailAsync(GetEmailRequest request) =>
            CallAsync<GetEmailRequest, GetEmailResponse>(request, "mail/get");

        public Task<DeleteEmailResponse> DeleteEmailAsync(DeleteEmailRequest request) =>
            CallAsync<DeleteEmailRequest, DeleteEmailResponse>(request, "mail/delete");

        public Task<DeleteAllEmailsResponse> DeleteAllEmailsAsync(DeleteAllEmailsRequest request) =>
            CallAsync<DeleteAllEmailsRequest, DeleteAllEmailsResponse>(request, "mail/delete-all");

        public Task<CreateEmailResponse> CreateEmailAsync(CreateEmailRequest request) =>
            CallAsync<CreateEmailRequest, CreateEmailResponse>(request, "mail/create");

        public Task<TestSmtpResponse> TestSmtpAsync(TestSmtpRequest request) =>
            CallAsync<TestSmtpRequest, TestSmtpResponse>(request, "mail/test-smtp");
    }
}