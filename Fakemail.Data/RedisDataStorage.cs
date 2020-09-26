using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Fakemail.DataModels;
using Fakemail.Models;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

namespace Fakemail.Data
{
    internal static class Extensions
    {
        public static string ReversedMailbox(this EmailAddress emailAddress)
        {
            return string.Join('@', emailAddress.Mailbox.Split('@').Reverse());
        }
    }

    internal class RedisLogger : TextWriter
    {
        private ILogger<RedisDataStorage> _log;

        public override Encoding Encoding => throw new NotImplementedException();

        public RedisLogger(ILogger<RedisDataStorage> log)
        {
            _log = log;
        }

        public override void WriteLine(string s)
        {
            _log.LogDebug(s);
        }
    }

    public class RedisDataStorage : IDataStorage
    {
        private ILogger<RedisDataStorage> _log;
        private IConnectionMultiplexer _redis;
        private IRedisConfiguration _configuration;

        public RedisDataStorage(ILogger<RedisDataStorage> log, IRedisConfiguration configuration)
        {
            _log = log;
            _configuration = configuration;

            var redisOptions = new ConfigurationOptions
            {
                Password = configuration.Password
            };
            redisOptions.EndPoints.Add(configuration.Host, configuration.Port);

            _redis = ConnectionMultiplexer.Connect(redisOptions, new RedisLogger(_log));
        }

        private IDatabase Database => _redis.GetDatabase(_configuration.DatabaseNumber);

        public async Task CreateMessage(Message message, MessageSummary messageSummary, IEnumerable<EmailAddress> toEmailAddresses)
        {
            var transaction = Database.CreateTransaction();
            var transactionTasks = new List<Task>();

            // Store the full message
            var messageKey = $"message:{message.Id}";
            _log.LogDebug($"Queuing command: Set key '{messageKey}'");
            transactionTasks.Add(transaction.StringSetAsync(messageKey, message.Content, TimeSpan.FromDays(7)));

            // Store the message summary: timestamp, from, to, first 80 chars of subject, first 80 chars of text body
            // Put an exipry on this, so that these are the second things to be deleted
            var messageSummaryKey = $"message-summary:{message.Id}";
            _log.LogDebug($"Queuing command: Set key '{messageSummaryKey}'");
            var messageSummaryJson = JsonSerializer.Serialize(messageSummary);
            transactionTasks.Add(transaction.StringSetAsync(messageSummaryKey, messageSummaryJson, TimeSpan.FromDays(30)));

            foreach (var toEmailAddress in toEmailAddresses)
            {
                // reverse the 'to' address so the domain comes first, allowing us to retrieve by domain
                var reversedAddress = toEmailAddress.ReversedMailbox();

                // ensure the mailbox address is created
                var mailboxAddressesKey = "mailbox-addresses";
                _log.LogDebug($"Queuing command: Add '{reversedAddress}' to key '{mailboxAddressesKey}'");
                transactionTasks.Add(transaction.SortedSetAddAsync(mailboxAddressesKey, reversedAddress, 0, CommandFlags.None));

                // add the message ID to each mailbox
                var mailboxIndexKey = $"mailbox-index:{reversedAddress}";
                _log.LogDebug($"Queuing command: Add '{message.Id}' to key '{mailboxIndexKey}'");
                transactionTasks.Add(transaction.SortedSetAddAsync(mailboxIndexKey, message.Id, 0, CommandFlags.None));

                transactionTasks.Add(transaction.PublishAsync("message-received", $"{toEmailAddress.Mailbox}:{messageSummaryJson}"));
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

        public async Task<bool> MailboxExists(EmailAddress address)
        {
            _log.LogDebug("Checking if mailbox exists for {address}", address.Mailbox);

            var score = await Database.SortedSetScoreAsync("mailbox-addresses", address.ReversedMailbox());

            return score != null;
        }

        public async Task<bool> CreateMailboxAsync(EmailAddress address)
        {
            _log.LogDebug("Creating mailbox {mailbox}", address.Mailbox);

            return await Database.SortedSetAddAsync("mailbox-addresses", address.ReversedMailbox(), 0, When.NotExists, CommandFlags.None);
        }

        public Task DeleteMailbox(string mailboxName)
        {
            throw new NotImplementedException();
        }

        public void AddSubscription(Action<string, MessageSummary> action)
        {
            // message is mailbox:messageSummary
            _redis.GetSubscriber().Subscribe("message-received", (channel, message) =>
            {
                try
                {
                    var parts = message.ToString().Split(":", 2);
                    var mailbox = parts[0];
                    var messageSummary = JsonSerializer.Deserialize<MessageSummary>(parts[1]);
                    action(mailbox, messageSummary);
                }
                catch (Exception e)
                {
                    _log.LogError(e, "Failed to add subscription");
                }
            });
        }

        public async Task<List<MessageSummary>> GetMessageSummaries(EmailAddress address, int skip, int take)
        {
            if (!await MailboxExists(address))
                throw new Exception("Mailbox not found");

            var mailboxIndexKey = $"mailbox-index:{address.ReversedMailbox()}";
            var messageIds = await Database.SortedSetRangeByValueAsync(mailboxIndexKey, "0000000000-0", "9999999999-0", order: Order.Descending, skip: skip, take: take);

            var summaries = new List<MessageSummary>();
            foreach (var id in messageIds)
            {
                var messageSummaryKey = $"message-summary:{id}";
                string summary = await Database.StringGetAsync(messageSummaryKey);
                try
                {
                    summaries.Add(JsonSerializer.Deserialize<MessageSummary>(summary));
                }
                catch (Exception)
                {
                    _log.LogError($"Error deserializing message summary {id}", id);
                }
            }
            return summaries;
        }
    }
}
