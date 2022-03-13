using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Fakemail.DataModels;

namespace Fakemail.Data
{
    public interface IDataStorage
    {
        Task<(User, string)> CreateUserAsync(string username, string salt, string hashedPassword);

        Task<User> ReadUserAsync(string username);

        Task DeleteUserAsync(string username);

        Task CreateEmailAsync(string username, DateTime receivedTimestamp, string from, string[] to, string subject, string textBody, Attachment[] attachments);

        Task DeleteEmailAsync(string username, string messageId);

        Task<List<Email>> ReadEmailsAsync(string username, int skip, int take);
    }
}
