using SmtpServer;
using SmtpServer.Storage;

namespace Fakemail.Smtp
{
    public class MessageStoreFactory : IMessageStoreFactory
    {
        private IMessageStore _messageStore;

        public MessageStoreFactory(IMessageStore messageStore)
        {
            _messageStore = messageStore;
        }

        public IMessageStore CreateInstance(ISessionContext context)
        {
            return _messageStore;
        }
    }
}
