
using Fakemail.ApiModels;

namespace Fakemail.Web
{
    public interface IFakemailApi
    {
        Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request);
        Task<ListEmailsResponse> ListEmailsAsync(ListEmailsRequest request);
    }
}