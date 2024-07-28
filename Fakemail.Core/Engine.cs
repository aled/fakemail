using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Fakemail.ApiModels;
using Fakemail.Data.EntityFramework;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using MimeKit;

using static Fakemail.Cryptography.Sha2Crypt;

using DataAttachment = Fakemail.Data.EntityFramework.Attachment;
using DataEmail = Fakemail.Data.EntityFramework.Email;
using DataSmtpUser = Fakemail.Data.EntityFramework.SmtpUser;
using DataUser = Fakemail.Data.EntityFramework.User;

namespace Fakemail.Core
{
    public partial class Engine(
        IDbContextFactory<FakemailDbContext> dbFactory,
        ILogger<Engine> log,
        IJwtAuthentication auth,
        TimeProvider timeProvider,
        IPwnedPasswordApi pwnedPasswordApi) : IEngine
    {
        [GeneratedRegex(@"^from (?<ReceivedFromHost>.*) \((?<ReceivedFromDns>.*) \[(?<ReceivedFromIp>.*)\]\)\s+by (?<ReceivedByHost>.*) \((?<SmtpdName>.*)\) with (?<SmtpIdType>.*) id (?<SmtpId>.*) \((?<TlsInfo>TLS.*)\) auth=yes user=(?<SmtpUsername>[a-zA-Z0-9]+)(\s+for <(?<ReceivedFor>.*)>)?;\s(?<ReceivedWeekday>.*), (?<ReceivedDay>.*) (?<ReceivedMonth>.*) (?<ReceivedYear>.*) (?<ReceivedTime>.*) (?<ReceivedTimeOffset>.*) \((?<ReceivedTimezone>.*)\)$")]
        private static partial Regex ReceivedHeaderRegex();

        /// <summary>
        /// Create a user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request)
        {
            var userId = new Guid(RandomNumberGenerator.GetBytes(16));
            var password = request.Password;
            try
            {
                // Validate username, if present
                if (request.Username == null)
                {
                    request.Username = $"anon-{userId}";
                }
                else if (request.Username.Length < 6)
                {
                    return new CreateUserResponse
                    {
                        ErrorMessage = "Username length must be at least 6 characters"
                    };
                }
                else if (request.Username.Length > 40)
                {
                    return new CreateUserResponse
                    {
                        ErrorMessage = "Username length must not be greater than 40 characters"
                    };
                }
                // Validate password
                if (password == null)
                {
                    // No password, so will create an unsecured account. No validation necessary.
                }
                else if (password.Length < 10)
                {
                    // Password must be at least 10 chars but maximum 40
                    return new CreateUserResponse
                    {
                        ErrorMessage = "Password length must be at least 10 characters"
                    };
                }
                else if (password.Length > 40)
                {
                    return new CreateUserResponse
                    {
                        ErrorMessage = "Password length must not be greater than 40 characters"
                    };
                }
                else
                {
                    // Password must not be in HaveIBeenPwned list of compromised passwords
                    if (await pwnedPasswordApi.IsPwnedPasswordAsync(password))
                    {
                        return new CreateUserResponse
                        {
                            ErrorMessage = "Password was found in HaveIBeenPwned"
                        };
                    }
                }

                var smtpUsernameBytes = 4;
                var smtpUsername = Utils.CreateId(smtpUsernameBytes).ToLower(); // SMTP auth fails if upper-case chars are used
                var smtpPassword = Utils.CreateId(8);
                var passwordCrypt = password == null ? string.Empty : Sha256Crypt(password);

                using (var db = dbFactory.CreateDbContext())
                {
                    var dataUser = new DataUser
                    {
                        UserId = userId,
                        Username = request.Username,
                        PasswordCrypt = passwordCrypt
                    };

                    await db.Users.AddAsync(dataUser);

                    while (db.SmtpUsers.Any(u => u.SmtpUsername == smtpUsername))
                    {
                        smtpUsername = Utils.CreateId(smtpUsernameBytes).ToLower(); // SMTP auth fails if upper-case chars are used
                    }

                    var dataSmtpUser = new DataSmtpUser
                    {
                        UserId = dataUser.UserId,
                        SmtpUsername = smtpUsername,
                        SmtpPassword = smtpPassword,
                        SmtpPasswordCrypt = Sha256Crypt(smtpPassword)
                    };

                    await db.SmtpUsers.AddAsync(dataSmtpUser);
                    await db.SaveChangesAsync();
                }

                return new CreateUserResponse
                {
                    Success = true,
                    ErrorMessage = null,
                    UserId = userId,
                    Username = request.Username,
                    SmtpUsername = smtpUsername,
                    SmtpPassword = smtpPassword
                };
            }
            catch (DbUpdateException due)
            {
                if (due.InnerException is SqliteException se)
                {
                    if (se.SqliteErrorCode == 19)
                    {
                        log.LogError("Failed to create user - already exists");
                        return new CreateUserResponse
                        {
                            ErrorMessage = "User already exists"
                        };
                    }
                    log.LogError("Failed to update database: {message}", se.Message);
                }
                else
                {
                    log.LogError("Failed to update database: {message}", due.Message);
                }
            }
            catch (Exception ex)
            {
                log.LogError("Exception: {message}", ex.Message);
            }

            return new CreateUserResponse
            {
                ErrorMessage = "Server error",
            };
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
                using var db = dbFactory.CreateDbContext();

                var user = await db.Users
                    .Where(x => x.UserId == request.UserId)
                    .SingleAsync();

                if (Validate(request.Password, user.PasswordCrypt))
                {
                    response.Success = true;
                    response.ErrorMessage = null;
                    response.IsAdmin = user.IsAdmin;
                    response.Token = auth.GetAuthenticationToken(user.UserId, user.IsAdmin);
                }
            }
            catch (Exception e)
            {
                log.LogError("Exception in GetTokenAsync: {message}", e.Message);
            }

            return response;
        }

        private static string Extract(GroupCollection groups, string groupName)
        {
            var value = groups[groupName];

            if (value.Success)
            {
                return value.Value;
            }

            throw new Exception($"Failed to parse '{groupName}'");
        }

        public async Task<CreateEmailResponse> CreateEmailAsync(CreateEmailRequest request, Guid authenticatedUserId)
        {
            using (var db = dbFactory.CreateDbContext())
            {
                var user = await GetAuthenticatedOrUnsecuredUserAsync(db, request, authenticatedUserId);

                if (user == null)
                {
                    return new CreateEmailResponse
                    {
                        Success = false,
                        ErrorMessage = "Unauthorized"
                    };
                }
            }

            using var stream = new MemoryStream(request.MimeMessage);
            return await CreateEmailAsync(stream);
        }

        /// <summary>
        /// This method is used to insert new messages into the database that have been delivered via
        /// the SMTP server.
        /// </summary>
        /// <param name="messageStream"></param>
        /// <returns></returns>
        public async Task<CreateEmailResponse> CreateEmailAsync(Stream messageStream)
        {
            try
            {
                // read the message into a memory buffer, so we can create the MimeMessage from it, and also
                // access the underlying buffer to store the raw message in the database
                var stream = new MemoryStream();
                await messageStream.CopyToAsync(stream);

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
                var receivedHeaderRegex = ReceivedHeaderRegex();

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
                    attachments = [];
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
                    SequenceNumber = 0,
                    From = m.Headers["From"] ?? "",
                    To = m.Headers["To"] ?? "",
                    CC = m.Headers["CC"] ?? "",
                    DeliveredTo = m.Headers["Delivered-To"] ?? "",
                    Subject = m.Headers["Subject"] ?? "",
                    BodyLength = m.TextBody?.Length ?? 0,
                    BodySummary = m.TextBody?.Length <= 50 ? m.TextBody : m.TextBody?[..50] ?? "",
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

                using var db = dbFactory.CreateDbContext();
                using var transaction = db.Database.BeginTransaction(IsolationLevel.ReadCommitted);

                var smtpUser = await db.SmtpUsers
                    .Where(x => x.SmtpUsername == smtpUsername)
                    .SingleAsync();

                var currentSequenceNumber = smtpUser.CurrentEmailSequenceNumber;

                email.SequenceNumber = currentSequenceNumber + 1;
                smtpUser.CurrentEmailSequenceNumber = email.SequenceNumber;
                smtpUser.CurrentEmailReceivedTimestampUtc = email.ReceivedTimestampUtc;

                await db.Emails.AddAsync(email);
                if (attachments != null)
                {
                    await db.Attachments.AddRangeAsync(attachments);
                }
                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return new CreateEmailResponse
                {
                    Success = true,
                    EmailId = emailId
                };
            }
            catch (Exception e)
            {
                log.LogError("Exception in CreateMailAsync: {message}\n{stacktrace}", e.Message, e.StackTrace);

                return new CreateEmailResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to create email"
                };
            }
        }

        public async Task<TestSmtpResponse> TestSmtpAsync(TestSmtpRequest request, Guid authenticatedUserId, SmtpServer smtpServer)
        {
            SmtpUser smtpUser = null;

            using (var db = dbFactory.CreateDbContext())
            {
                var user = await GetAuthenticatedOrUnsecuredUserAsync(db, request, authenticatedUserId);

                if (user == null)
                {
                    return new TestSmtpResponse
                    {
                        Success = false,
                        ErrorMessage = "Unauthorized"
                    };
                }

                smtpUser = await db.SmtpUsers.FirstOrDefaultAsync(su => su.UserId == request.UserId && su.SmtpUsername == request.SmtpUsername);

                if (smtpUser == null)
                {
                    return new TestSmtpResponse
                    {
                        Success = false,
                        ErrorMessage = "Unknown SMTP username"
                    };
                }
            }

            try
            {
                var message = new MailMessage();

                if (request.Email.From != null) message.From = new MailAddress(request.Email.From);
                if (request.Email.Sender != null) message.Sender = new MailAddress(request.Email.Sender);
                message.Subject = request.Email.Subject;
                message.Body = request.Email.Body;

                static void Add(string[] source, MailAddressCollection dest)
                {
                    foreach (var s in source ?? [])
                    {
                        dest.Add(new MailAddress(s));
                    }
                }
                Add(request.Email.To, message.To);
                Add(request.Email.Cc, message.CC);
                Add(request.Email.Bcc, message.Bcc);

                foreach (var attachment in request.Email.Attachments ?? [])
                {
                    var contentStream = new MemoryStream(attachment.Content);
                    message.Attachments.Add(new System.Net.Mail.Attachment(contentStream, attachment.Filename, attachment.ContentType));
                }

                smtpUser.SmtpUsername = "1utrb0";
                smtpUser.SmtpPassword = "eBXoZO4GEwV";

                var smtpClient = new SmtpClient(smtpServer.Host, smtpServer.Port)
                {
                    EnableSsl = true,

                    UseDefaultCredentials = false,

                    Credentials = new NetworkCredential(smtpUser.SmtpUsername, smtpUser.SmtpPassword)
                };

                await smtpClient.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                return new TestSmtpResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }

            return new TestSmtpResponse
            {
                Success = true
            };
        }

        public async Task<ListUserResponse> ListUsersAsync(ListUserRequest request)
        {
            using var db = dbFactory.CreateDbContext();
            var users = await db.Users
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new
                {
                    x.Username,
                    x.IsAdmin,
                    SmtpUsers = x.SmtpUsers.Select(y => new SmtpUserDetail
                    {
                        SmtpPassword = null,
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="db"></param>
        /// <param name="authenticatedUserId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private static async Task<User> GetAuthenticatedOrUnsecuredUserAsync(FakemailDbContext db, IUserRequest request, Guid authenticatedUserId)
        {
            var requestedUser = await db.Users.SingleOrDefaultAsync(x => x.UserId == request.UserId);

            if (requestedUser != null)
            {
                // if requested user is unsecured, allow access
                if (requestedUser.PasswordCrypt == string.Empty)
                {
                    return requestedUser;
                }

                // if requested user is the current authenticated user, allow access
                else if (requestedUser.UserId == authenticatedUserId)
                {
                    return requestedUser;
                }

                // if requested user is different from authenticated user, authenticated user must be an admin
                else if (await db.Users.AnyAsync(x => x.UserId == authenticatedUserId && x.IsAdmin))
                {
                    return requestedUser;
                }
            }
            return null;
        }

        public async Task<ListEmailsBySequenceNumberResponse> ListEmailsBySequenceNumberAsync(ListEmailsBySequenceNumberRequest request, Guid authenticatedUserId)
        {
            var response = new ListEmailsBySequenceNumberResponse
            {
                Success = false,
            };

            using var db = dbFactory.CreateDbContext();
            var user = await GetAuthenticatedOrUnsecuredUserAsync(db, request, authenticatedUserId);

            if (user == null)
            {
                response.ErrorMessage = "Unauthorized";
                return response;
            }

            response.Success = true;

            response.Emails = await db.Emails
                .Where(e => e.SmtpUsername == request.SmtpUsername)
                .Where(e => e.SequenceNumber >= request.MinSequenceNumber)
                .OrderByDescending(e => e.ReceivedTimestampUtc)
                .ThenByDescending(e => e.SequenceNumber)
                .Take(request.LimitEmailCount)
                .Select(e => new EmailSummary
                {
                    EmailId = e.EmailId,
                    SequenceNumber = e.SequenceNumber,
                    SmtpUsername = e.SmtpUsername,
                    TimestampUtc = e.ReceivedTimestampUtc,
                    From = e.From,
                    Subject = e.Subject,
                    DeliveredTo = e.DeliveredTo,
                    BodySummary = e.BodySummary,
                    Attachments = (from a in db.Attachments
                                   where a.EmailId == e.EmailId
                                   select new AttachmentSummary
                                   {
                                       AttachmentId = a.AttachmentId,
                                       Name = a.Filename
                                   }).ToList()
                }).ToListAsync();

            response.Username = user.Username;

            return response;
        }

        /// <summary>
        /// Unsecured users may list emails from only their own SMTP usernames.
        /// Secured users may list emails from only their own SMTP usernames.
        /// Admin users may list emails from any SMTP username.
        /// </summary>
        /// <param name="authenticatedUsername"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ListEmailsResponse> ListEmailsAsync(ListEmailsRequest request, Guid authenticatedUserId)
        {
            var response = new ListEmailsResponse
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

            using var db = dbFactory.CreateDbContext();
            var user = await GetAuthenticatedOrUnsecuredUserAsync(db, request, authenticatedUserId);

            if (user == null)
            {
                response.ErrorMessage = "Unauthorized";
                return response;
            }

            var smtpUsersDetail = await db.SmtpUsers
                                         .Where(su => su.UserId == user.UserId)
                                         .Select(su => new SmtpUserDetail
                                         {
                                             SmtpUsername = su.SmtpUsername,
                                             SmtpPassword = su.SmtpPassword,
                                             CurrentEmailCount = su.Emails.Count()
                                         })
                                         .ToListAsync();

            var emails = (from e in db.Emails
                          join su in db.SmtpUsers on e.SmtpUsername equals su.SmtpUsername
                          join u in db.Users on su.UserId equals u.UserId
                          where u.UserId == user.UserId
                          select e);

            var emailsCount = await emails.CountAsync();
            if (emailsCount == 0)
            {
                response.Success = true;
                response.Emails = [];
                response.Username = user.Username;
                response.SmtpUsers = smtpUsersDetail;
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
                .ThenByDescending(e => e.SequenceNumber)
                .ThenBy(e => e.EmailId)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(e => new EmailSummary
                {
                    EmailId = e.EmailId,
                    SequenceNumber = e.SequenceNumber,
                    SmtpUsername = e.SmtpUsername,
                    TimestampUtc = e.ReceivedTimestampUtc,
                    From = e.From,
                    Subject = e.Subject,
                    DeliveredTo = e.DeliveredTo,
                    BodySummary = e.BodySummary,
                    Attachments = (from a in db.Attachments
                                   where a.EmailId == e.EmailId
                                   select new AttachmentSummary
                                   {
                                       AttachmentId = a.AttachmentId,
                                       Name = a.Filename
                                   }).ToList()
                }).ToListAsync();

            response.Page = request.Page;
            response.PageSize = request.PageSize;
            response.Username = user.Username;
            response.SmtpUsers = smtpUsersDetail;
            response.Success = true;

            return response;
        }

        public async Task<GetEmailResponse> GetEmailAsync(GetEmailRequest request, Guid authenticatedUserId)
        {
            using var db = dbFactory.CreateDbContext();
            var user = await GetAuthenticatedOrUnsecuredUserAsync(db, request, authenticatedUserId);

            if (user == null)
            {
                return new GetEmailResponse
                {
                    Success = false,
                    ErrorMessage = "Unauthorized"
                };
            }

            var email = await db.Emails
                .Where(x => x.EmailId == request.EmailId && x.SmtpUser.UserId == user.UserId)
                .FirstOrDefaultAsync();

            if (email == null)
            {
                return new GetEmailResponse
                {
                    Success = false,
                    ErrorMessage = "Not found"
                };
            }

            return new GetEmailResponse
            {
                Success = true,
                Bytes = email.MimeMessage,
                Filename = $"email-{email.EmailId}.eml"
            };
        }

        public async Task<DeleteEmailResponse> DeleteEmailAsync(DeleteEmailRequest request, Guid authenticatedUserId)
        {
            using var db = dbFactory.CreateDbContext();
            var user = await GetAuthenticatedOrUnsecuredUserAsync(db, request, authenticatedUserId);

            if (user == null)
            {
                return new DeleteEmailResponse
                {
                    Success = false,
                    ErrorMessage = "Unauthorized"
                };
            }

            var emailIdToDelete = await db.Emails
                .Where(x => x.EmailId == request.EmailId && x.SmtpUser.UserId == user.UserId)
                .Select(x => x.EmailId)
                .FirstOrDefaultAsync();

            if (emailIdToDelete == Guid.Empty)
            {
                return new DeleteEmailResponse
                {
                    Success = false,
                    ErrorMessage = "Not found"
                };
            }

            db.Remove(new DataEmail { EmailId = emailIdToDelete });
            await db.SaveChangesAsync();

            return new DeleteEmailResponse
            {
                Success = true
            };
        }

        public async Task<DeleteAllEmailsResponse> DeleteAllEmailsAsync(DeleteAllEmailsRequest request, Guid authenticatedUserId)
        {
            using var db = dbFactory.CreateDbContext();
            var user = await GetAuthenticatedOrUnsecuredUserAsync(db, request, authenticatedUserId);

            if (user == null)
            {
                return new DeleteAllEmailsResponse
                {
                    Success = false,
                    ErrorMessage = "Unauthorized"
                };
            }

            var smtpUsername = db.SmtpUsers
                .Where(x => x.UserId == user.UserId)
                .Where(x => x.SmtpUsername == request.SmtpUsername)
                .Select(x => x.SmtpUsername)
                .FirstOrDefault();

            int count = 0;
            if (smtpUsername != null)
            {
                count = await db.Emails
                    .Where(e => e.SmtpUsername == smtpUsername)
                    .ExecuteDeleteAsync();
            }

            return new DeleteAllEmailsResponse
            {
                Success = true,
                EmailDeletedCount = count
            };
        }

        public async Task<CleanupEmailsResponse> CleanupEmailsAsync(CleanupEmailsRequest request, CancellationToken cancellationToken)
        {
            var maxEmailAge = TimeSpan.FromSeconds(request.MaxEmailAgeSeconds);
            var deleteEmailsEarlierThan = timeProvider.GetUtcNow().Subtract(maxEmailAge);

            var batchSize = 1000;

            using var db = dbFactory.CreateDbContext();
            var emailsDeleted = 0;
            var totalEmailsDeleted = 0;

            // keep emails newer than the maxEmailAge; delete others
            do
            {
                FormattableString sql = $"DELETE FROM Email WHERE ReceivedTimestampUtc < {deleteEmailsEarlierThan}";
                emailsDeleted = await db.Database.ExecuteSqlAsync(sql, cancellationToken);
                totalEmailsDeleted += emailsDeleted;
            } while (emailsDeleted > 0);

            // keep newest N emails per user; delete others
            do
            {
                // TODO: This, but in EF with a single roundtrip. (Is it even possible?)
                FormattableString sql = @$"WITH t1 AS (SELECT emailId, ROW_NUMBER() OVER (PARTITION BY smtpUsername ORDER BY ReceivedTimestampUtc DESC, SequenceNumber DESC) AS row_number FROM email),
     t2 AS (SELECT emailId FROM t1 WHERE row_number > {request.MaxEmailCount} LIMIT {batchSize})
DELETE FROM email WHERE emailId IN (SELECT emailId FROM t2)";

                emailsDeleted = await db.Database.ExecuteSqlAsync(sql, cancellationToken);

                totalEmailsDeleted += emailsDeleted;
            } while (emailsDeleted > 0);

            return new CleanupEmailsResponse
            {
                Success = true,
                TotalEmailsDeleted = totalEmailsDeleted
            };
        }
    }
}
