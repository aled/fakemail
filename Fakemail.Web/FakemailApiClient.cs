using Microsoft.Extensions.Options;

using Fakemail.ApiModels;

namespace Fakemail.Web
{
    public class FakemailApiClient(HttpClient httpClient, IOptions<FakemailApiClientOptions> options) : IFakemailApiClient
    {
        private readonly FakemailApiClientOptions _options = options.Value;

        private async Task<TResp> CallAsync<TReq, TResp>(TReq req, string path)
        {
            var response = await httpClient.PostAsJsonAsync($"{_options.BaseUri}{path}", req);
            return await response.Content.FromJsonAsync<TResp>();
        }

        public Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request) =>
            CallAsync<CreateUserRequest, CreateUserResponse>(request, "user/create");

        public Task<ListEmailsResponse> ListEmailsAsync(ListEmailsRequest request) =>
            CallAsync<ListEmailsRequest, ListEmailsResponse>(request, "mail/list");
            
        public Task<ListEmailsBySequenceNumberResponse> ListEmailsBySequenceNumberAsync(ListEmailsBySequenceNumberRequest request) =>
            CallAsync<ListEmailsBySequenceNumberRequest, ListEmailsBySequenceNumberResponse>(request, "mail/listbysequencenumber");

        public Task<GetEmailResponse> GetEmailAsync(GetEmailRequest request) =>
            CallAsync<GetEmailRequest, GetEmailResponse>(request, "mail/get");

        public Task<DeleteEmailResponse> DeleteEmailAsync(DeleteEmailRequest request) =>
            CallAsync<DeleteEmailRequest, DeleteEmailResponse>(request, "mail/delete");

        public Task<DeleteAllEmailsResponse> DeleteAllEmailsAsync(DeleteAllEmailsRequest request) =>
            CallAsync<DeleteAllEmailsRequest, DeleteAllEmailsResponse>(request, "mail/deleteall");

        public Task<CreateEmailResponse> CreateEmailAsync(CreateEmailRequest request) =>
            CallAsync<CreateEmailRequest, CreateEmailResponse>(request, "mail/create");
    }
}