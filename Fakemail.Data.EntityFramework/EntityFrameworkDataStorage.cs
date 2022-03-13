using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using Fakemail.DataModels;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fakemail.Data.EntityFramework
{
    public class EntityFrameworkDataStorage : IDataStorage
    {
        private ILogger<EntityFrameworkDataStorage> _log;

        public EntityFrameworkDataStorage(ILogger<EntityFrameworkDataStorage> log)
        {
            _log = log;
        }

        public async Task<(User, string)> CreateUserAsync(string username, string salt, string hashedPassword)
        {
            try
            {
                var error = string.Empty;

                using (var db = new FakemailDataContext())
                {
                    var user = new User
                    {
                        Username = username,
                        Salt = salt,
                        HashedPassword = hashedPassword
                    };
                    await db.Users.AddAsync(user);
                    await db.SaveChangesAsync();

                    return (user, error);
                }
            }
            catch (DbUpdateException dbe)
            {
                if (dbe.InnerException is SqliteException se)
                {
                    if (se.SqliteErrorCode == 19 && se.Message.Contains("UNIQUE constraint failed: Users.Username"))
                    {
                        return (null, "User already exists");
                    }
                    else
                    {
                        return (null, $"Unhandled database error: " + se.Message);
                    }
                }
                else
                {
                    return (null, $"Unhandled database error: " + dbe.Message);
                }
            }
        }

        public async Task<User> ReadUserAsync(string username)
        {
            using (var db = new FakemailDataContext())
            {
                return await db.Users.FirstOrDefaultAsync(x => x.Username == username);
            }
        }

        public Task DeleteUserAsync(string username)
        {
            throw new NotImplementedException();
        }

        public async Task<Email> CreateEmailAsync(string username, string from, string[] to, string subject, string textBody)
        {
            var user = await ReadUserAsync(username);

            if (user == null)
                throw new Exception("Invalid username");

            var email = new Email
            { 
                UserId = user.UserId,
                From = from,
                To = string.Join("; ", to),
                Subject = subject,
                TextBody = textBody
            };

            using (var db = new FakemailDataContext())
            {
                await db.Emails.AddAsync(email);
                await db.SaveChangesAsync();
            }

            return email;
        }

        public Task<List<Email>> ReadEmailsAsync(string username, int skip, int take)
        {
            using (var db = new FakemailDataContext())
            {
                return (from u in db.Users where u.Username == username
                        join e in db.Emails
                        on u.UserId equals e.UserId
                        select e)
                        .Skip(skip)
                        .Take(take)
                        .ToListAsync();
            }
        }

        public async Task CreateEmailAsync(string username, DateTime receivedTimestamp, string from, string[] to, string subject, string textBody, Attachment[] attachments)
        {
            var user = await ReadUserAsync(username);

            if (user == null)
                throw new Exception("Invalid user");

            var email = new Email
            {
                UserId = user.UserId,
                ReceivedTimestamp = DateTime.UtcNow,
                From = from,
                To = string.Join("; ", to),
                Subject = subject,
                TextBody = textBody,
                Attachments = attachments
            };

            using (var db = new FakemailDataContext())
            {
                await db.Emails.AddAsync(email);
                await db.SaveChangesAsync();
            }
        }

        public Task DeleteEmailAsync(string username, string messageId)
        {
            throw new NotImplementedException();
        }

    }
}
