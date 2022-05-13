using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using MimeKit;

using Fakemail.ApiModels;
using Fakemail.Data.EntityFramework;

using static Fakemail.Cryptography.Sha2Crypt;

using DataAttachment = Fakemail.Data.EntityFramework.Attachment;
using DataEmail = Fakemail.Data.EntityFramework.Email;
using DataSmtpUser = Fakemail.Data.EntityFramework.SmtpUser;
using DataUser = Fakemail.Data.EntityFramework.User;

namespace Fakemail.Core
{
    public class Engine : IEngine
    {
        private IDbContextFactory<FakemailDbContext> _dbFactory;
        private ILogger<Engine> _log;
        private IJwtAuthentication _auth;
        private IPwnedPasswordApi _pwnedPasswordApi;

        public Engine(IDbContextFactory<FakemailDbContext> dbFactory, ILogger<Engine> log, IJwtAuthentication auth, IPwnedPasswordApi pwnedPasswordApi )
        {
            _dbFactory = dbFactory;
            _log = log;
            _auth = auth;
            _pwnedPasswordApi = pwnedPasswordApi;
        }

        /// <summary>
        /// Create a user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request)
        {
            var response = new CreateUserResponse
            {
                Success = false,
                ErrorMessage = "Error creating account"
            };

            var serverCreatedPassword = false;
            var password = request.Password;
            try
            {
                // Validate username
                if (request.Username == null || request.Username.Length < 6)
                {
                    response.ErrorMessage = "Username length must be at least 6 characters";
                    return response;
                }
                else if (request.Username.Length > 30)
                {
                    response.ErrorMessage = "Username length must not be greater than 30 characters";
                    return response;
                }

                // Validate password
                if (password == null)
                {
                    // If not specified, create one automatically.
                    // Retry if listed in HaveIBeenPwned
                    do
                    {
                        password = Utils.CreateId(10);  // will return a 14 char password
                    }
                    while (await _pwnedPasswordApi.IsPwnedPasswordAsync(password));

                    serverCreatedPassword = true;
                }
                else if (password.Length < 10)
                {
                    // Password must be at least 10 chars but maximum 40
                    response.ErrorMessage = "Password length must be at least 10 characters";
                    return response;
                }
                else if (password.Length > 40)
                {
                    response.ErrorMessage = "Password length must not be greater than 40 characters";
                    return response;
                }
                else
                {
                    // Password must not be in HaveIBeenPwned list of compromised passwords
                    if (await _pwnedPasswordApi.IsPwnedPasswordAsync(password))
                    {
                        response.ErrorMessage = "Password was found in HaveIBeenPwned";
                        return response;
                    }
                }

                var userId = new Guid(RandomNumberGenerator.GetBytes(16));

                var smtpUsernameBytes = 4;
                var smtpUsername = Utils.CreateId(smtpUsernameBytes).ToLower(); // SMTP auth fails if upper-case chars are used
                var smtpPassword = Utils.CreateId(8);

                var dataUser = new DataUser
                {
                    UserId = userId,
                    Username = request.Username,
                    PasswordCrypt = Sha256Crypt(password)
                };

                var dataSmtpUser = new DataSmtpUser
                {
                    UserId = dataUser.UserId,
                    SmtpUsername = smtpUsername,
                    SmtpPasswordCrypt = Sha256Crypt(smtpPassword)
                };

                using (var db = _dbFactory.CreateDbContext())
                {
                    while (db.SmtpUsers.Any(u => u.SmtpUsername == smtpUsername))
                    {
                        smtpUsername = Utils.CreateId(smtpUsernameBytes).ToLower(); // increase the length in case of collision
                    }
                    await db.Users.AddAsync(dataUser);
                    await db.SmtpUsers.AddAsync(dataSmtpUser);
                    await db.SaveChangesAsync();
                }

                response.Success = true;
                response.ErrorMessage = null;
                response.Username = request.Username;
                if (serverCreatedPassword) response.Password = password;
                response.SmtpUsername = smtpUsername;
                response.SmtpPassword = smtpPassword;
                response.BearerToken = _auth.GetAuthenticationToken(request.Username, false);
            }
            catch (DbUpdateException due)
            {
                if (due.InnerException is SqliteException se)
                {
                    _log.LogError(se.Message);
                    if (se.SqliteErrorCode == 19)
                    {
                        response.ErrorMessage = "User already exists";
                    }
                }
                else
                {
                    _log.LogError(due.Message);
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = "Server error";
                response.Username = null;
                response.Password = null;
                response.SmtpPassword = null;
                response.SmtpUsername = null;
                response.BearerToken = null;
                _log.LogError(ex.Message);
            }

            return response;
        }

        /// <summary>
        /// Validate a user
        /// </summary>
        /// <param name="apiUser"></param>
        /// <returns></returns>
        public async Task<GetTokenResponse> GetTokenAsync(GetTokenRequest request)
        {
            var response = new GetTokenResponse
            {
                Success = false,
                ErrorMessage = "Invalid username or password"
            };

            try
            {
                using (var db = _dbFactory.CreateDbContext())
                {
                    var user = await db.Users
                        .Where(x => x.Username == request.Username)
                        .SingleAsync();

                    if (Validate(request.Password, user.PasswordCrypt))
                    {
                        response.Success = true;
                        response.ErrorMessage = null;
                        response.IsAdmin = user.IsAdmin;
                        response.Token = _auth.GetAuthenticationToken(user.Username, user.IsAdmin);
                    }
                }
            }
            catch (Exception e)
            {
                _log.LogError(e.Message);
            }

            return response;
        }

        private string Extract(GroupCollection groups, string groupName)
        {
            var value = groups[groupName];

            if (value.Success)
            {
                return value.Value;
            }

            throw new Exception($"Failed to parse '{groupName}'");
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

                // Remove the headers that are specific to the envelope
                //stream.Position = 0;
                //var m2 = MimeMessage.Load(stream);

                //m.Headers.Remove("Message-Id");
                //m.Headers.Remove("Delivered-To");

                //m.WriteTo()

                // Extract out the parts of the message
                // * from
                // * to
                // * cc
                // * bcc
                // * delivered-to
                // * subject
                // * received date, other fields.
                // * smtp username

                // Parse the 'Received header'. This is fragile and may break on updates of the SMTP server.

                // from examplehost (static-123-234-12-23.example.co.uk [123.234.12.23])
                //      by fakemail.stream (OpenSMTPD) with ESMTPSA id 392ecef5 (TLSv1.2:ECDHE-RSA-AES256-GCM-SHA384:256:NO) auth=yes user=user234;" +
                //      Fri, 29 Apr 2022 21:28:41 +0000 (UTC)
                var receivedHeaderRegex = new Regex(
                    @"^from (?<ReceivedFromHost>.*) \((?<ReceivedFromDns>.*) \[(?<ReceivedFromIp>.*)\]\)" +
                    @"\s+by (?<ReceivedByHost>.*) \((?<SmtpdName>.*)\) with (?<SmtpIdType>.*) id (?<SmtpId>.*) \((?<TlsInfo>TLS.*)\) auth=yes user=(?<SmtpUsername>[a-zA-Z0-9]+)" +
                    @"(\s+for <(?<ReceivedFor>.*)>)?;" +
                    @"\s(?<ReceivedWeekday>.*), (?<ReceivedDay>.*) (?<ReceivedMonth>.*) (?<ReceivedYear>.*) (?<ReceivedTime>.*) (?<ReceivedTimeOffset>.*) \((?<ReceivedTimezone>.*)\)$");

                var match = receivedHeaderRegex.Match(m.Headers["Received"]);

                var receivedDay = Extract(match.Groups, "ReceivedDay");
                var receivedMonth = Extract(match.Groups, "ReceivedMonth");
                var receivedYear = Extract(match.Groups, "ReceivedYear");
                var receivedTime = Extract(match.Groups, "ReceivedTime");
                var receivedTimeOffset = Extract(match.Groups, "ReceivedTimeOffset");

                var receivedTimestamp = DateTimeOffset.Parse($"{receivedYear}-{receivedMonth}-{receivedDay} {receivedTime}{receivedTimeOffset}");

                var emailId = new Guid(RandomNumberGenerator.GetBytes(16));

                var smtpUsername = Extract(match.Groups, "SmtpUsername");

                List<DataAttachment> attachments = null;
                if (m.Attachments != null)
                {
                    attachments = new List<DataAttachment>();
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
                            Filename = x.ContentType?.Name
                        });
                    }
                }

                var email = new DataEmail
                {
                    EmailId = emailId,
                    From = m.Headers["From"] ?? "",
                    To = m.Headers["To"] ?? "",
                    CC = m.Headers["CC"] ?? "",
                    DeliveredTo = m.Headers["Delivered-To"] ?? "",
                    Subject = m.Headers["Subject"] ?? "",
                    BodyLength = m.TextBody?.Length ?? 0,
                    BodySummary = m.TextBody?.Length <= 50 ? m.TextBody : m.TextBody?.Substring(0, 50) ?? "",
                    MimeMessage = stream.GetBuffer(),
                    BodyChecksum = Utils.Checksum(m.TextBody ?? ""),
                    ReceivedFromDns = match.Groups["ReceivedFromDns"]?.Value,
                    ReceivedFromHost = match.Groups["ReceivedFromHost"]?.Value,
                    ReceivedFromIp = match.Groups["ReceivedFromIp"]?.Value,
                    ReceivedSmtpId = match.Groups["SmtpId"]?.Value,
                    ReceivedTlsInfo = match.Groups["TlsInfo"]?.Value,
                    SmtpUsername = smtpUsername,
                    ReceivedTimestampUtc = new DateTime(receivedTimestamp.UtcTicks),
                    Attachments = attachments
                };

                using (var db = _dbFactory.CreateDbContext())
                {
                    await db.Emails.AddAsync(email);
                    if (attachments != null)
                    {
                        await db.Attachments.AddRangeAsync(attachments);
                    }
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                _log.LogError(e.Message);
                _log.LogError(e.StackTrace);
                return false;
            }

            return true;
        }

        public async Task<ListUserResponse> ListUsersAsync(ListUserRequest request)
        {
            using (var db = _dbFactory.CreateDbContext())
            {
                var users = await db.Users
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(x => new
                    {
                        x.Username,
                        x.IsAdmin,
                        SmtpUsers = x.SmtpUsers.Select(y => new SmtpUserDetail
                        {
                            SmtpUsername = y.SmtpUsername,
                            CurrentEmailCount = y.Emails.Count
                        }),
                    })
                    .Select(x => new UserDetail
                    {
                        Username = x.Username,
                        IsAdmin = x.IsAdmin,
                        SmtpUsers = x.SmtpUsers.ToList(),
                        CurrentEmailCount = x.SmtpUsers.Sum(y => y.CurrentEmailCount)
                    })
                    .ToListAsync();

                return new ListUserResponse
                {
                    Success = true,
                    ErrorMessage = null,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    Users = users
                };
            }
        }

        /// <summary>
        /// Regular users may list emails from only their own SMTP usernames.
        /// Admin users may list emails from any SMTP username.
        /// </summary>
        /// <param name="authenticatedUsername"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ListEmailResponse> ListEmailsAsync(string authenticatedUsername, ListEmailRequest request)
        {
            var response = new ListEmailResponse
            {
                Success = false,
            };

            if (request.PageSize <= 0 || request.PageSize > 100)
            {
                response.ErrorMessage = "Invalid page size";

                return response;
            }

            if (request.Page <= 0)
            {
                response.Success = false;
                response.ErrorMessage = "Invalid page";

                return response;
            }

            using (var db = _dbFactory.CreateDbContext())
            {
                var authenticatedUser = await db.Users
                    .SingleOrDefaultAsync(x => x.Username == authenticatedUsername);

                var authorized = false;
                if (authenticatedUser == null)
                {
                    authorized = false;
                }
                else if (authenticatedUser.IsAdmin)
                {
                    authorized = true;
                }
                else
                {
                    authorized = await db.SmtpUsers
                        .AnyAsync(u => u.SmtpUsername == request.SmtpUsername && u.UserId == authenticatedUser.UserId);
                }

                if (!authorized)
                {
                    response.ErrorMessage = "Unauthorized SMTP username for authorized user";

                    return response;
                }

                var emails = db.Emails
                    .Where(x => x.SmtpUsername == request.SmtpUsername);

                var emailsCount = await emails.CountAsync();
                if (emailsCount == 0)
                {
                    response.Success = true;
                    response.Emails = new List<EmailSummary>();

                    return response;
                }

                response.MaxPage = ((emailsCount - 1) / request.PageSize) + 1;

                if (request.Page > response.MaxPage)
                {
                    response.Success = false;
                    response.ErrorMessage = "Invalid page";

                    return response;
                }

                response.Emails = await emails
                    .OrderByDescending(e => e.ReceivedTimestampUtc)
                    .ThenByDescending(e => e.EmailId)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(e => new EmailSummary
                    {
                        EmailId = e.EmailId,
                        SmtpUsername = e.SmtpUsername,
                        Timestamp = e.ReceivedTimestampUtc,
                        Subject = e.Subject,
                        DeliveredTo = e.DeliveredTo,
                        BodySummary = e.BodySummary,
                        Attachments = (from a in db.Attachments where a.EmailId == e.EmailId
                                       select new AttachmentSummary
                                       {
                                           AttachmentId = a.AttachmentId,
                                           Name = a.Filename
                                       }).ToList()
                    }).ToListAsync();

                response.Page = request.Page;
                response.PageSize = request.PageSize;

                response.Success = true;
                
                return response;
            }
        }
    } 
}
