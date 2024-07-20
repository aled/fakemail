
using Fakemail.ApiModels;

namespace Fakemail.Web
{
    public interface IFakemailApiClient
    {
        Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request);
        Task<ListEmailsResponse> ListEmailsAsync(ListEmailsRequest request);
        Task<ListEmailsBySequenceNumberResponse> ListEmailsBySequenceNumberAsync(ListEmailsBySequenceNumberRequest request);
        Task<GetEmailResponse> GetEmailAsync(GetEmailRequest request);
        Task<DeleteEmailResponse> DeleteEmailAsync(DeleteEmailRequest request);
        Task<DeleteAllEmailsResponse> DeleteAllEmailsAsync(DeleteAllEmailsRequest request);
        Task<CreateEmailResponse> CreateEmailAsync(CreateEmailRequest createEmailRequest);
    }
}