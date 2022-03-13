using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Fakemail.ApiModels;

using MimeKit;

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
        
        Task<AuthenticateUserResult> AuthenticateUserAsync(string username, string password);

        Task<ListEmailResult> ReadEmailsAsync(string username, string password, int skip, int take);

        Task OnEmailReceivedAsync(string username, string from, IEnumerable<string> to, IReadOnlyDictionary<string, string> parameters, MimeMessage message);
    }
}
