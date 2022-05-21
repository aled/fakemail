
using Fakemail.ApiModels;

namespace Fakemail.Web
{
    public interface IFakemailApi
    {
        Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request);
        Task<ListEmailsResponse> ListEmailsAsync(ListEmailsRequest request);
        Task<GetEmailResponse> GetEmailAsync(GetEmailRequest request);
        Task<DeleteEmailResponse> DeleteEmailAsync(DeleteEmailRequest request);
        Task<CreateEmailResponse> CreateEmailAsync(CreateEmailRequest createEmailRequest);
    }
}