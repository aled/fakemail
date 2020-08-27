using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MimeKit;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;

using Fakemail.Core;

namespace Fakemail.Smtp
{
    public class MessageStore : IMessageStore
    {
        private IEngine _engine;

        public MessageStore(IEngine engine)
        {
            _engine = engine;
        }

        public async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken)
        {
            try
            {
                var textMessage = (ITextMessage)transaction.Message;
                var message = MimeMessage.Load(textMessage.Content);

                await _engine.OnEmailReceivedAsync(transaction.From.AsAddress(), transaction.To.Select(x => x.AsAddress()), transaction.Parameters, message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);

                throw;
            }
            return SmtpResponse.Ok;
        }
    }
}
