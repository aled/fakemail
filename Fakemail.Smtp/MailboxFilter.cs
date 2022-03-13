using System.Threading;
using System.Threading.Tasks;

using Fakemail.Core;

using Microsoft.Extensions.Logging;

using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Storage;

namespace Fakemail.Smtp
{
    public class MailboxFilter : IMailboxFilter
    {
        private readonly ILogger<MailboxFilter> _log;

        private readonly IEngine _engine;

        public MailboxFilter(ILogger<MailboxFilter> log, IEngine engine)
        {
            _engine = engine;
            _log = log;
        }

        public Task<MailboxFilterResult> CanAcceptFromAsync(ISessionContext context, IMailbox from, int size, CancellationToken cancellationToken)
        {
            return Task.FromResult(MailboxFilterResult.Yes);
        }

        public Task<MailboxFilterResult> CanDeliverToAsync(ISessionContext context, IMailbox to, IMailbox from, CancellationToken cancellationToken)
        {
            return Task.FromResult(MailboxFilterResult.Yes);
        }
    }
}
