using System;
using System.IO;
using System.Threading.Tasks;

using Fakemail.ApiModels;

namespace Fakemail.Core
{
    public interface IEngine
    {
        /// <summary>
        /// Create a user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request);

        Task<GetTokenResponse> GetTokenAsync(GetTokenRequest user);

        Task<bool> CreateEmailAsync(Stream messageStream);

        Task<ListUserResponse> ListUsersAsync(ListUserRequest request);

        Task<ListEmailResponse> ListEmailsAsync(string authenticatedUsername, ListEmailRequest request);
    }
}
