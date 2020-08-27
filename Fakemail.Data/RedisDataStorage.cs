using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using StackExchange.Redis;

using Fakemail.DataModels;
using Fakemail.Models;
using Serilog;

namespace Fakemail.Data
{
    static class Extensions
    {
        public static string ReversedMailbox(this EmailAddress emailAddress)
        {
            return string.Join('@', emailAddress.Mailbox.Split('@').Reverse());
        }
    }

    public class RedisDataStorage : IDataStorage
    {
        private ILogger _log;
        private IConnectionMultiplexer _redis;
        private IRedisConfiguration _configuration;

        public RedisDataStorage(ILogger logger, IRedisConfiguration configuration)
        {
            _log = logger.ForContext<RedisDataStorage>();
            _configuration = configuration;
            
            var redisOptions = new ConfigurationOptions
            {
                Password = configuration.Password
            };
            redisOptions.EndPoints.Add(configuration.Host, configuration.Port);

            _redis = ConnectionMultiplexer.Connect(redisOptions);
        }
        
        private IDatabase Database =>_redis.GetDatabase(_configuration.DatabaseNumber);
   
        public async Task CreateMessage(Message message, MessageSummary messageSummary, IEnumerable<EmailAddress> toEmailAddresses)
        {
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

            foreach (var toEmailAddress in toEmailAddresses)
            {
                // reverse the 'to' address so the domain comes first, allowing us to retrieve by domain
                var reversedAddress = toEmailAddress.ReversedMailbox();

                // ensure the mailbox address is created
                var mailboxAddressesKey = "mailbox-addresses";
                Console.WriteLine($"Queuing command: Add '{reversedAddress}' to key '{mailboxAddressesKey}'");
                transactionTasks.Add(transaction.SortedSetAddAsync(mailboxAddressesKey, reversedAddress, 0, CommandFlags.None));

                // add the message ID to each mailbox
                var mailboxIndexKey = $"mailbox-index:{reversedAddress}";
                Console.WriteLine($"Queuing command: Add '{message.Id}' to key '{mailboxIndexKey}'");
                transactionTasks.Add(transaction.SortedSetAddAsync(mailboxIndexKey, message.Id, 0, CommandFlags.None));
            }

            if (!await transaction.ExecuteAsync())
            {
                throw new Exception();
            }

            await Task.WhenAll(transactionTasks.ToArray());
        }

        public Task DeleteMessage(string messageId)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> MailboxExists(EmailAddress emailAddress)
        {
            _log.Information("Checking if mailbox exists for {address}", emailAddress.Mailbox);

            _log.Information("Quering redis for value {value} in sorted set 'mailbox-addresses'", emailAddress.ReversedMailbox());

            return (await Database.SortedSetScoreAsync("mailbox-addresses", emailAddress.ReversedMailbox())) != null;
        }

        public Task CreateMailbox(string mailboxName)
        {
            throw new NotImplementedException();
        }

        public Task DeleteMailbox(string mailboxName)
        {
            throw new NotImplementedException();
        }

        public IObservable<MessageSummary> ObserveMessageSummaries(string mailboxName, DateTime from)
        {
            throw new NotImplementedException();
        }
    }
}
