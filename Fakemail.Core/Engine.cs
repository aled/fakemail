using Fakemail.Data;
using Fakemail.Models;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

        private IDataStorage _dataStorage;

        public Engine(IDataStorage dataStorage)
        {
            _dataStorage = dataStorage;
        }

        private static string GenerateMessageId(DateTime timestamp)
        {
            long seconds = (timestamp - DateTime.UnixEpoch).Ticks / TICKS_PER_SECOND;
            string uniquifier = Guid.NewGuid().ToString().Replace("-", "");

            return $"{seconds:0000000000}-{uniquifier}";
        }

        public async Task OnEmailReceivedAsync(string fromMailbox, IEnumerable<string> toMailboxes, IReadOnlyDictionary<string, string> parameters, MimeMessage mimeMessage)
        {
            var messageTimestamp = DateTime.UtcNow;
            string messageId = GenerateMessageId(messageTimestamp);

            Console.WriteLine($"message received: timestamp='{messageTimestamp}', from.length={mimeMessage.From?.Count.ToString() ?? "NULL"}");
            
            Console.WriteLine(mimeMessage.TextBody);

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

            var messageSummaryModel = new MessageSummary(
                messageId,
                messageTimestamp,
                mimeMessage.From[0].ToString(),
                mimeMessage.Subject.Truncate(80),
                mimeMessage.TextBody.Truncate(80));

            var to = toMailboxes.Select(x => x.Split("@"))
                .Where(split => split.Length == 2)
                .Select(split => new string(split[0].TakeWhile(x => x != '+').ToArray()) + "@" + split[1])
                .ToArray();

            await _dataStorage.CreateMessage(messageModel, messageSummaryModel, toMailboxes);
        }
    }
}
