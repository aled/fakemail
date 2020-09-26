using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Fakemail.Core;

using Microsoft.Extensions.Logging;

using MimeKit;

using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace Fakemail.Smtp
{
    public class MessageStore : IMessageStore
    {
        private ILogger<MessageStore> _log;
        private IEngine _engine;

        public MessageStore(ILogger<MessageStore> log, IEngine engine)
        {
            _log = log;
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
                _log.LogError(e, "Failed to save smtp message");
                return SmtpResponse.TransactionFailed;
            }
            return SmtpResponse.Ok;
        }
    }
}
