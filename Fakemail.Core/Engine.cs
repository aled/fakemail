using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using MimeKit;

using Fakemail.Data;
using Fakemail.DataModels;
using Fakemail.Models;
using Serilog;

namespace Fakemail.Core
{
    static class Extensions
    {
        public static string Truncate(this string s, int len)
        {
            if (s == null)
                return "";

            if (s.Length <= len)
                return s;

            return s.Substring(0, len);
        }
    }

    public class Engine : IEngine
    {
        private static readonly long TICKS_PER_SECOND = 10000000;

        private ILogger _log;
        private IDataStorage _dataStorage;

        public Engine(ILogger logger, IDataStorage dataStorage)
        {
            _log = logger.ForContext<Engine>();
            _dataStorage = dataStorage;
        }

        private static string GenerateMessageId(DateTime timestamp)
        {
            long seconds = (timestamp - DateTime.UnixEpoch).Ticks / TICKS_PER_SECOND;
            string uniquifier = Guid.NewGuid().ToString().Replace("-", "");

            return $"{seconds:0000000000}-{uniquifier}";
        }

        public async Task<CreateMailboxResult> CreateMailboxAsync(string mailbox = null)
        {
            var result = new CreateMailboxResult();

            if (mailbox == null)
                mailbox = Guid.NewGuid().ToString() + "@fakemail.stream";

            if (EmailAddress.TryParse(mailbox, out var validatedAddress))
            {
                result.Mailbox = validatedAddress.Mailbox;
                if (await _dataStorage.CreateMailboxAsync(validatedAddress))
                    result.Success = true;
                else
                {
                    result.Success = true;
                    result.ErrorMessage = "Mailbox already exists";
                }   
            }
            else
            {
                result.ErrorMessage = $"Invalid email address";
            }

            return result;
        }

        public async Task<bool> MailboxExistsAsync(string emailAddress)
        {
            _log.Information("Parsing email address {emailAddress}", emailAddress);

            if (EmailAddress.TryParse(emailAddress, out var validatedEmailAddress))
                return await _dataStorage.MailboxExists(validatedEmailAddress);

            return false;
        }

        public async Task OnEmailReceivedAsync(string fromMailbox, IEnumerable<string> toEmailAddresses, IReadOnlyDictionary<string, string> parameters, MimeMessage mimeMessage)
        {
            var messageTimestamp = DateTime.UtcNow;
            string messageId = GenerateMessageId(messageTimestamp);

            Console.WriteLine($"message received: timestamp='{messageTimestamp}'");

            Directory.CreateDirectory("/tmp/fakemail");
            await File.WriteAllTextAsync($"/tmp/fakemail/{messageId}.body.txt", mimeMessage.TextBody);

            byte[] messageContent = null;
            using (var s = new MemoryStream())
            {
                mimeMessage.WriteTo(s);
                messageContent = s.ToArray();
            }

            var messageModel = new Message(
                messageId,
                messageTimestamp,
                messageContent);

            var messageSummaryModel = new MessageSummary
            {
                Id = messageId,
                ReceivedTimestamp = messageTimestamp,
                From = mimeMessage.From[0].ToString(),
                Subject = mimeMessage.Subject.Truncate(80),
                Body = mimeMessage.TextBody.Truncate(80)
            };

            var validatedAddresses = new List<EmailAddress>();
            foreach (var a in toEmailAddresses)
                if (EmailAddress.TryParse(a, out EmailAddress validatedAdress))
                    validatedAddresses.Add(validatedAdress);

            await _dataStorage.CreateMessage(messageModel, messageSummaryModel, validatedAddresses);
        }

        public async Task<IList<MessageSummary>> GetMessageSummaries(string emailAddress, int skip, int take)
        {
            if (!EmailAddress.TryParse(emailAddress, out var validatedAddress))
            {
                throw new Exception("Invalid email address");
            }

            return await _dataStorage.GetMessageSummaries(validatedAddress, skip, take);
        }

        public void AddSubscription(Action<string, MessageSummary> action)
        {
            _dataStorage.AddSubscription(action);
        }
    }
}
