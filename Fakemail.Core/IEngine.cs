using System.Collections.Generic;
using System.Threading.Tasks;

using MimeKit;

namespace Fakemail.Core
{
    public interface IEngine
    {
        Task<bool> MailboxExistsAsync(string emailAddress);

        Task OnEmailReceivedAsync(string from, IEnumerable<string> to, IReadOnlyDictionary<string, string> parameters, MimeMessage message);
    }
}
