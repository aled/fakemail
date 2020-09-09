using System.Threading;
using System.Threading.Tasks;

using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Storage;

using Fakemail.Core;

namespace Fakemail.Smtp
{
    public class MailboxFilter : IMailboxFilter
    {
        private readonly Serilog.ILogger _log;

        private readonly IEngine _engine;

        public MailboxFilter(Serilog.ILogger logger, IEngine engine)
        {
            _engine = engine;
            _log = logger.ForContext<MailboxFilter>();
        }

        public Task<MailboxFilterResult> CanAcceptFromAsync(ISessionContext context, IMailbox from, int size, CancellationToken cancellationToken)
        {
            return Task.FromResult(MailboxFilterResult.Yes);
        }

        public async Task<MailboxFilterResult> CanDeliverToAsync(ISessionContext context, IMailbox to, IMailbox from, CancellationToken cancellationToken)
        {
            if (to.Host == "fakemail.stream")
                if (await _engine.MailboxExistsAsync(to.AsAddress()))
                    return MailboxFilterResult.Yes;

            _log.Information("No mailbox exists for {address}", to.AsAddress());
            return MailboxFilterResult.NoPermanently;
        }
    }
}
