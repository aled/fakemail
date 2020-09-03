using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Fakemail.DataModels;
using Fakemail.Models;

namespace Fakemail.Data
{
    public interface IDataStorage
    {
        Task CreateMessage(Message message, MessageSummary messageSummary, IEnumerable<EmailAddress> toEmailAddresses);

        Task DeleteMessage(string messageId);

        Task<bool> MailboxExists(EmailAddress emailAddress);

        Task<bool> CreateMailboxAsync(EmailAddress emailAddress);

        Task DeleteMailbox(string mailboxName);

        Task<List<MessageSummary>> GetMessageSummaries(EmailAddress mailbox, int skip, int take);

        void AddSubscription(Action<string, MessageSummary> action);
    }
}
