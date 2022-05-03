using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

using DataUser = Fakemail.Data.EntityFramework.User;
using DataSmtpUser = Fakemail.Data.EntityFramework.SmtpUser;
using DataEmail = Fakemail.Data.EntityFramework.Email;
using DataAttachment = Fakemail.Data.EntityFramework.Attachment;

using ApiUser = Fakemail.ApiModels.User;


using Fakemail.ApiModels;
using Fakemail.Data.EntityFramework;

using static Fakemail.Cryptography.Sha2Crypt;
using System.IO;
using MimeKit;
using System.Collections.Generic;

namespace Fakemail.Core
{
    public class Engine : IEngine
    {
        private ILogger<Engine> _log;
        private IDbContextFactory<FakemailDbContext> _dbFactory;

        public Engine(IDbContextFactory<FakemailDbContext> dbFactory, ILogger<Engine> log)
        {
            _log = log;
            _dbFactory = dbFactory;
        }

        /// <summary>
        /// Create a user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<CreateUserResult> CreateUserAsync(ApiUser user)
        {
            var result = new CreateUserResult
            {
                Success = false,
                ErrorMessage = "Error creating user"
            };

            try
            {
                // TODO: check quality of password

                using (var db = _dbFactory.CreateDbContext())
                {
                    var userId = new Guid(RandomNumberGenerator.GetBytes(16));
                    var smtpUsername = Utils.CreateId();
                    var smtpPassword = Utils.CreateId();

                    var dataUser = new DataUser
                    {
                        UserId = userId,
                        Username = user.Username,
                        PasswordCrypt = Sha512Crypt(user.Password)
                    };
                   
                    var dataSmtpUser = new DataSmtpUser
                    {
                        UserId = dataUser.UserId,
                        SmtpUsername = smtpUsername,
                        SmtpPasswordCrypt = Sha512Crypt(smtpPassword)
                    };

                    await db.Users.AddAsync(dataUser);
                    await db.SmtpUsers.AddAsync(dataSmtpUser);
                    await db.SaveChangesAsync();

                    result.Success = true;
                    result.Username = user.Username;
                    result.SmtpUsername = smtpUsername;
                    result.SmtpPassword = smtpPassword;                  
                }
            }
            catch (DbUpdateException due)
            {
                if (due.InnerException is SqliteException se)
                {
                    _log.LogError(se.Message);
                    if (se.SqliteErrorCode == 19)
                    {
                        result.ErrorMessage = "User already exists";
                    }
                }
                else
                {
                    _log.LogError(due.Message);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Validate a user
        /// </summary>
        /// <param name="apiUser"></param>
        /// <returns></returns>
        public async Task<AuthenticateUserResult> AuthenticateUserAsync(ApiUser apiUser)
        {
            var result = new AuthenticateUserResult
            {
                Success = false,
                ErrorMessage = "Invalid username or password"
            };

            try
            {
                using (var db = _dbFactory.CreateDbContext())
                {
                    var passwordCrypt = await db.Users
                        .Where(x => x.Username == apiUser.Username)
                        .Select(x => x.PasswordCrypt)
                        .SingleAsync();

                    if (Validate(apiUser.Password, passwordCrypt))
                    {
                        result.Success = true;
                        result.ErrorMessage = string.Empty;
                    }
                }
            }
            catch (Exception e)
            {
                _log.LogError(e.Message);
            }

            return result;        
        }

        /// <summary>
        /// This method is used to insert newly delivered messages into the database
        /// </summary>
        /// <param name="messageStream"></param>
        /// <returns></returns>
        public async Task<bool> CreateEmail(Stream messageStream)
        {
            try
            {
                var stream = new MemoryStream();
                messageStream.CopyTo(stream);

                stream.Position = 0;
                var m = MimeMessage.Load(stream);

                // Extract out the parts of the message
                // * from
                // * to
                // * cc
                // * bcc
                // * delivered-to
                // * subject
                // * received date
                // * smtp username
                var email = new DataEmail
                {
                    EmailId = new Guid(RandomNumberGenerator.GetBytes(16)),
                    From = m.Headers["From"],
                    To = m.Headers["To"],
                    DeliveredTo = m.Headers["Delivered-To"],
                    Subject = m.Subject,
                    BodyLength = m.TextBody.Length,
                    MimeMessage = stream.GetBuffer()
                };

                var attachments = new List<DataAttachment>();

             
                using (var db = _dbFactory.CreateDbContext())
                {
                    db.Emails.Add(email);
                    db.Attachments.AddRange(attachments);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                _log.LogError(e.Message);
                return false;
            }

            return true;

        }
    }
}
