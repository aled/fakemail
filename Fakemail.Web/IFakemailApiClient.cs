using Fakemail.ApiModels;

using Microsoft.Extensions.Options;

namespace Fakemail.Web
{
    public interface IFakemailApiClient
    {
        string ExternalBaseUri { get; }

        Task<GetSmtpServerResponse> GetSmtpServerAsync(GetSmtpServerRequest request);

        Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request);

        Task<ListEmailsResponse> ListEmailsAsync(ListEmailsRequest request);

        Task<ListEmailsBySequenceNumberResponse> ListEmailsBySequenceNumberAsync(ListEmailsBySequenceNumberRequest request);

        Task<GetEmailResponse> GetEmailAsync(GetEmailRequest request);

        Task<DeleteEmailResponse> DeleteEmailAsync(DeleteEmailRequest request);

        Task<DeleteAllEmailsResponse> DeleteAllEmailsAsync(DeleteAllEmailsRequest request);

        Task<CreateEmailResponse> CreateEmailAsync(CreateEmailRequest createEmailRequest);

        Task<TestSmtpResponse> TestSmtpAsync(TestSmtpRequest testSmtpRequest);
    }
}