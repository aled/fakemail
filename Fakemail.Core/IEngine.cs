using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Fakemail.Models;

using MimeKit;

namespace Fakemail.Core
{
    public interface IEngine
    {
        /// <summary>
        /// Create a mailbox with a random name.
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <returns></returns>
        Task CreateMailboxAsync(string emailAddress);

        Task<bool> MailboxExistsAsync(string emailAddress);

        Task OnEmailReceivedAsync(string from, IEnumerable<string> to, IReadOnlyDictionary<string, string> parameters, MimeMessage message);

        Task<IList<MessageSummary>> GetMessageSummaries(string emailAddress, int skip, int take);
    }
}
