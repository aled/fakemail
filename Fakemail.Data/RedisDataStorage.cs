using Fakemail.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fakemail.Data
{
    public class RedisDataStorage : IDataStorage
    {
        private IConnectionMultiplexer _redis { get; set; }

        public RedisDataStorage(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }
        
        private IDatabase Database =>_redis.GetDatabase(1);
        
        public async Task CreateMessage(Message message, MessageSummary messageSummary, IEnumerable<string> toMailboxes)
        {
            // reverse the 'to' addresses so the domain comes first, allowing us to retrieve by domain
            var reversedMailboxes = toMailboxes
                .Select(x => x.Split("@"))
                .Where(x => x.Length == 2)
                .Select(x => x[1] + "@" + x[0])
                .ToArray();

            var transaction = Database.CreateTransaction();
            var transactionTasks = new List<Task>();

            // Store the full message
            var messageKey = $"message:{message.Id}";
            Console.WriteLine($"Queuing command: Set key '{messageKey}'");
            transactionTasks.Add(transaction.StringSetAsync(messageKey, message.Content, TimeSpan.FromDays(7)));

            // Store the message summary: timestamp, from, to, first 80 chars of subject, first 80 chars of text body
            // Put an exipry on this, so that these are the second things to be deleted
            var messageSummaryKey = $"message-summary:{message.Id}";
            Console.WriteLine($"Queuing command: Set key '{messageSummaryKey}'");
            transactionTasks.Add(transaction.StringSetAsync(messageSummaryKey, JsonSerializer.Serialize(messageSummary), TimeSpan.FromDays(30)));

            foreach (var reversedAddress in reversedMailboxes)
            {                
                // ensure the mailbox address is created
                var mailboxAddressesKey = "mailbox-addresses";
                Console.WriteLine($"Queuing command: Add '{reversedAddress}'  to key '{mailboxAddressesKey}'");
                transactionTasks.Add(transaction.SortedSetAddAsync(mailboxAddressesKey, reversedAddress, 0, CommandFlags.None));

                // add the message ID to each mailbox
                var mailboxIndexKey = $"mailbox-index:{reversedAddress}";
                Console.WriteLine($"Queuing command: Add '{message.Id}'  to key '{mailboxIndexKey}'");
                transactionTasks.Add(transaction.SortedSetAddAsync(mailboxIndexKey, message.Id, 0, CommandFlags.None));
            }

            if (!await transaction.ExecuteAsync())
            {
                throw new Exception();
            }

            await Task.WhenAll(transactionTasks.ToArray());
        }

        public string CreateSession(string username)
        {
            throw new NotImplementedException();
        }

        public void DeleteMessage()
        {
            throw new NotImplementedException();
        }

        public void DeleteSession(string username, string sessionId)
        {
            throw new NotImplementedException();
        }

        public IObservable<MessageSummary> ObserveMessageSummaries(DateTime from)
        {
            throw new NotImplementedException();
        }
    }
}
