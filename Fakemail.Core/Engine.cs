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
using System.Text;
using System.Net.Mail;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Dynamic;

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
        public async Task<bool> CreateEmailAsync(Stream messageStream)
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
                // * received date, other fields.
                // * smtp username

                // parse the Received header 

                // from examplehost (static-123-234-12-23.example.co.uk [123.234.12.23])
                //      by fakemail.stream (OpenSMTPD) with ESMTPSA id 392ecef5 (TLSv1.2:ECDHE-RSA-AES256-GCM-SHA384:256:NO) auth=yes user=user234;" +
                //      Fri, 29 Apr 2022 21:28:41 +0000 (UTC)
                var receivedHeaderRegex0 = new Regex(@"^from (?<ReceivedFromHost>.*) \((?<ReceivedFromDns>.*) \[(?<ReceivedFromIp>.*)\]\)$");
                var receivedHeaderRegex1 = new Regex(@"^.*?by (?<ReceivedByHost>.*) \((?<SmtpdName>.*)\) with (?<SmtpIdType>.*) id (?<SmtpId>.*) \((?<TlsInfo>TLS.*)\) auth=yes user=(?<SmtpUsername>[a-zA-Z0-9]+);$");
                var receivedHeaderRegex2 = new Regex(@"^(?<ReceivedWeekday>.*), (?<ReceivedDay>.*) (?<ReceivedMonth>.*) (?<ReceivedYear>.*) (?<ReceivedTime>.*) (?<ReceivedTimeOffset>.*) \((?<ReceivedTimezone>.*)\)$");
                
                var receivedHeader = m.Headers["Received"].Split("        ", StringSplitOptions.RemoveEmptyEntries);
                var match0 = receivedHeaderRegex0.Match(receivedHeader[0]);
                var match1 = receivedHeaderRegex1.Match(receivedHeader[1]);
                var match2 = receivedHeaderRegex2.Match(receivedHeader[2]);

                var receivedDay = match2.Groups["ReceivedDay"].Value;
                var receivedMonth = match2.Groups["ReceivedMonth"].Value;
                var receivedYear = match2.Groups["ReceivedYear"].Value;
                var receivedTime = match2.Groups["ReceivedTime"].Value;
                var receivedTimeOffset = match2.Groups["ReceivedTimeOffset"].Value;

                var receivedTimestamp = DateTimeOffset.Parse($"{receivedYear}-{receivedMonth}-{receivedDay} {receivedTime}{receivedTimeOffset}");

                var emailId = new Guid(RandomNumberGenerator.GetBytes(16));

                var userId = new Guid(RandomNumberGenerator.GetBytes(16));

                var smtpUsername = match1.Groups["SmtpUsername"].Value;
                var smtpPassword = Utils.CreateId();

                var dataUser = new DataUser
                {
                    UserId = userId,
                    Username = Utils.CreateId(),
                    PasswordCrypt = Sha512Crypt(Utils.CreateId())
                };

                var dataSmtpUser = new DataSmtpUser
                {
                    SmtpUsername = smtpUsername,
                    UserId = dataUser.UserId,
                    SmtpPasswordCrypt = Sha512Crypt(smtpPassword)
                };

                var attachments = new List<DataAttachment>();
                foreach (var x in m.Attachments)
                {
                    var contentBytes = x.GetContentBytes();
                    attachments.Add(new DataAttachment
                    {
                        AttachmentId = new Guid(RandomNumberGenerator.GetBytes(16)),
                        EmailId = emailId,
                        Content = contentBytes,
                        ContentChecksum = Utils.Checksum(contentBytes),
                        ContentType = "",
                        Filename = x.ContentType.Name
                    });
                }

                var email = new DataEmail
                {
                    EmailId = emailId,
                    From = m.Headers["From"] ?? "",
                    To = m.Headers["To"] ?? "",
                    CC = m.Headers["CC"] ?? "",
                    DeliveredTo = m.Headers["Delivered-To"] ?? "",
                    Subject = m.Headers["Subject"] ?? "",
                    BodyLength = m.TextBody.Length,
                    BodySummary = m.TextBody.Length <= 50 ? m.TextBody : m.TextBody.Substring(0, 50),
                    MimeMessage = stream.GetBuffer(),
                    BodyChecksum = Utils.Checksum(m.TextBody),
                    ReceivedFromDns = match0.Groups["ReceivedFromDns"].Value,
                    ReceivedFromHost = match0.Groups["ReceivedFromHost"].Value,
                    ReceivedFromIp = match0.Groups["ReceivedFromIp"].Value,
                    ReceivedSmtpId = match1.Groups["SmtpId"].Value,
                    ReceivedTlsInfo = match1.Groups["TlsInfo"].Value,
                    SmtpUsername = smtpUsername,
                    ReceivedTimestamp = receivedTimestamp,
                    Attachments = attachments
                };

                using (var db = _dbFactory.CreateDbContext())
                {
                    await db.Users.AddAsync(dataUser);
                    await db.SmtpUsers.AddAsync(dataSmtpUser);
                    await db.Emails.AddAsync(email);
                    await db.Attachments.AddRangeAsync(attachments);
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
