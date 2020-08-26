using System.Collections.Generic;
using System.Threading.Tasks;
using MimeKit;

namespace Fakemail.Core
{
    public interface IEngine
    {
        public Task OnEmailReceivedAsync(string from, IEnumerable<string> to, IReadOnlyDictionary<string, string> parameters, MimeMessage message);
    }
}
