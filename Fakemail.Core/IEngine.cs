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
        Task<CreateUserResult> CreateUserAsync(User user);

        Task<AuthenticateUserResult> AuthenticateUserAsync(User user);

        Task<bool> CreateEmailAsync(Stream messageStream);
    }
}
