
using Fakemail.ApiModels;

using Microsoft.Extensions.Options;

namespace Fakemail.Web
{
    public class FakemailApi : IFakemailApi
    {
        private HttpClient _httpClient;
        private FakemailApiOptions _options;

        public FakemailApi(HttpClient httpClient, IOptions<FakemailApiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        private async Task<TResp> CallAsync<TReq, TResp>(TReq req, string path)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_options.BaseUri}{path}", req);
            return await response.Content.FromJsonAsync<TResp>();
        }

        public Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request) =>
            CallAsync<CreateUserRequest, CreateUserResponse>(request, "user/create");

        public Task<ListEmailsResponse> ListEmailsAsync(ListEmailsRequest request) =>
            CallAsync<ListEmailsRequest, ListEmailsResponse>(request, "mail/list");        
    }
}