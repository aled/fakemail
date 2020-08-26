using Fakemail.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fakemail.Data
{
    public interface IDataStorage
    {
        public string CreateSession(string username);

        public void DeleteSession(string username, string sessionId);

        public Task CreateMessage(Message message, MessageSummary messageSummary, IEnumerable<string> toMailboxes);

        public void DeleteMessage();

        public IObservable<MessageSummary> ObserveMessageSummaries(DateTime from);
    }
}
