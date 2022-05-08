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
using System.Net.Http;
using System.Text;

namespace Fakemail.Core
{
    public class Engine : IEngine
    {
        // For haveIBeenPwned - todo: move into a separate class
        private static HttpClient _httpClient = new HttpClient();

        private IDbContextFactory<FakemailDbContext> _dbFactory;
        private ILogger<Engine> _log;
        private IJwtAuthentication _auth;

        public Engine(IDbContextFactory<FakemailDbContext> dbFactory, ILogger<Engine> log, IJwtAuthentication auth)
        {
            _dbFactory = dbFactory;
            _log = log;
            _auth = auth;
        }

        /// <summary>
        /// Create a user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request)
        {
            var result = new CreateUserResponse
            {
                Success = false,
                ErrorMessage = "Error creating account"
            };

            try
            {
                // Check quality of password
                // 1. Must be at least 10 chars
                if (request.Password == null || request.Password.Length < 10)
                {
                    result.ErrorMessage = "Password must be at least 10 characters";
                    return result;
                }

                // 2. Must not be in HaveIGotPwned
                var sha1 = HashAlgorithm.Create("SHA1");
                var sha1PasswordBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(request.Password));
                var sb = new StringBuilder();

                // Loop through each byte of the hashed data
                // and format each one as a hexadecimal string.
                for (int i = 0; i < sha1PasswordBytes.Length; i++)
                {
                    sb.Append(sha1PasswordBytes[i].ToString("X2"));
                }

                var pwnedPasswordHashes = await _httpClient.GetStringAsync($"https://api.pwnedpasswords.com/range/{sb.ToString().Substring(0, 5)}");

                if (pwnedPasswordHashes.Contains(sb.ToString().Substring(5)))
                {
                    result.ErrorMessage = "Password was listed in HaveIBeenPwned";
                    return result;
                }

                var userId = new Guid(RandomNumberGenerator.GetBytes(16));

                var smtpUsernameBytes = 4;
                var smtpUsername = Utils.CreateId(smtpUsernameBytes);
                var smtpPassword = Utils.CreateId(8);

                var dataUser = new DataUser
                {
                    UserId = userId,
                    Username = request.Username,
                    PasswordCrypt = Sha512Crypt(request.Password)
                };

                var dataSmtpUser = new DataSmtpUser
                {
                    UserId = dataUser.UserId,
                    SmtpUsername = smtpUsername,
                    SmtpPasswordCrypt = Sha512Crypt(smtpPassword)
                };

                using (var db = _dbFactory.CreateDbContext())
                {
                    while (db.SmtpUsers.Any(u => u.SmtpUsername == smtpUsername))
                    {
                        smtpUsername = Utils.CreateId(smtpUsernameBytes); // increase the length in case of collision
                    }
                    await db.Users.AddAsync(dataUser);
                    await db.SmtpUsers.AddAsync(dataSmtpUser);
                    await db.SaveChangesAsync();
                }

                result.Success = true;
                result.ErrorMessage = null;
                result.Username = request.Username;
                result.SmtpUsername = smtpUsername;
                result.SmtpPassword = smtpPassword;
                result.BearerToken = _auth.GetAuthenticationToken(request.Username, false);
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
                result.ErrorMessage = "Server error";
                _log.LogError(ex.Message);
            }

            return result;
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
                var receivedHeaderRegex0 = new Regex(@"^from (?<ReceivedFromHost>.*) \((?<ReceivedFromDns>.*) \[(?<ReceivedFromIp>.*)\]\)$");
                var receivedHeaderRegex1 = new Regex(@"^.*?by (?<ReceivedByHost>.*) \((?<SmtpdName>.*)\) with (?<SmtpIdType>.*) id (?<SmtpId>.*) \((?<TlsInfo>TLS.*)\) auth=yes user=(?<SmtpUsername>[a-zA-Z0-9]+);$");
                var receivedHeaderRegex2 = new Regex(@"^(?<ReceivedWeekday>.*), (?<ReceivedDay>.*) (?<ReceivedMonth>.*) (?<ReceivedYear>.*) (?<ReceivedTime>.*) (?<ReceivedTimeOffset>.*) \((?<ReceivedTimezone>.*)\)$");

                var receivedHeader = m.Headers["Received"].Split(new[] { "        ", "\t" }, StringSplitOptions.RemoveEmptyEntries);

                if (receivedHeader.Length != 3)
                {
                    throw new Exception("Failed to parse 'Received' header");
                }

                var match0 = receivedHeaderRegex0.Match(receivedHeader[0]);
                var match1 = receivedHeaderRegex1.Match(receivedHeader[1]);
                var match2 = receivedHeaderRegex2.Match(receivedHeader[2]);

                var receivedDay = Extract(match2.Groups, "ReceivedDay");
                var receivedMonth = Extract(match2.Groups, "ReceivedMonth");
                var receivedYear = Extract(match2.Groups, "ReceivedYear");
                var receivedTime = Extract(match2.Groups, "ReceivedTime");
                var receivedTimeOffset = Extract(match2.Groups, "ReceivedTimeOffset");

                var receivedTimestamp = DateTimeOffset.Parse($"{receivedYear}-{receivedMonth}-{receivedDay} {receivedTime}{receivedTimeOffset}");

                var emailId = new Guid(RandomNumberGenerator.GetBytes(16));

                var smtpUsername = Extract(match1.Groups, "SmtpUsername");

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
                    ReceivedTimestampUtc = new DateTime(receivedTimestamp.UtcTicks),
                    Attachments = attachments
                };

                using (var db = _dbFactory.CreateDbContext())
                {
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
                Page = request.Page,
                PageSize = request.PageSize,
            };

            if (request.PageSize <= 0 || request.PageSize > 100)
            {
                response.Page = 0;
                response.PageSize = 0;
                response.ErrorMessage = "Invalid page size";

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
                    response.Page = 0;
                    response.PageSize = 0;
                    response.ErrorMessage = "Unauthorized SMTP username for authorized user";

                    return response;
                }

                var emails = db.Emails
                    .Where(x => x.SmtpUsername == request.SmtpUsername);

                var emailsCount = await emails.CountAsync();
                response.MaxPage = ((emailsCount - 1) / request.PageSize) + 1;

                if (emailsCount == 0)
                {
                    response.Success = true;
                    response.Page = 0;
                    response.Emails = new List<EmailSummary>();

                    return response;
                }

                if (request.Page > response.MaxPage)
                {
                    response.Success = false;
                    response.ErrorMessage = "Invalid page";
                    response.Page = 0;
                    //response.Emails = new List<EmailSummary>();

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

                response.Success = true;
                
                return response;
            }
        }
    } 
}
