using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Fakemail.DataModels;
using Fakemail.Models;

namespace Fakemail.Data
{
    public interface IDataStorage
    {
        public Task CreateMessage(Message message, MessageSummary messageSummary, IEnumerable<EmailAddress> toEmailAddresses);
        
        public Task DeleteMessage(string messageId);

        public Task<bool> MailboxExists(EmailAddress emailAddress);

        public Task CreateMailbox(string mailboxName);

        public Task DeleteMailbox(string mailboxName);
              
        public IObservable<MessageSummary> ObserveMessageSummaries(string mailboxName, DateTime from);
    }
}
