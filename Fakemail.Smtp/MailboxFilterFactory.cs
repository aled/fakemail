using SmtpServer;
using SmtpServer.Storage;

namespace Fakemail.Smtp
{
    public class MailboxFilterFactory : IMailboxFilterFactory
    {
        private IMailboxFilter _mailboxFilter;

        public MailboxFilterFactory(IMailboxFilter mailboxFilter)
        {
            _mailboxFilter = mailboxFilter;
        }

        public IMailboxFilter CreateInstance(ISessionContext context)
        {
            return _mailboxFilter;
        }
    }
}
