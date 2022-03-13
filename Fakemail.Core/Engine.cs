using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Fakemail.Data;

using DataUser = Fakemail.DataModels.User;
using DataEmail = Fakemail.DataModels.Email;
using DataAttachment = Fakemail.DataModels.Attachment;

using ApiUser = Fakemail.ApiModels.User;
using ApiEmail = Fakemail.ApiModels.Email;
using ApiAttachment = Fakemail.ApiModels.Attachment;

using Microsoft.Extensions.Logging;

using MimeKit;
using Fakemail.ApiModels;

namespace Fakemail.Core
{
    internal static class Extensions
    {
        public static string Truncate(this string s, int len)
        {
            if (s == null)
                return "";

            if (s.Length <= len)
                return s;

            return s.Substring(0, len);
        }
    }

    public class Engine : IEngine
    {
        private ILogger<Engine> _log;
        private IDataStorage _dataStorage;

        public Engine(ILogger<Engine> log, IDataStorage dataStorage)
        {
            _log = log;
            _dataStorage = dataStorage;
        }

        private static RandomNumberGenerator RandomSource = RandomNumberGenerator.Create();

        private static byte[] Random(int bytes)
        {
            var buf = new byte[bytes];

            lock (RandomSource)
            {
                RandomSource.GetBytes(buf);
            }

            return buf;
        }

        private string GenerateSalt()
        {
            var iterations = 2000;
            var salt = Random(16);

            return iterations.ToString("X") + "." + Convert.ToBase64String(salt);
        }

        private string HashedPassword(string password, string salt)
        {
            var i = salt.IndexOf('.');
            var iterations = int.Parse(salt.Substring(0, i), NumberStyles.HexNumber);
            salt = salt.Substring(i + 1);

            using (var pbkdf2 = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(password), Convert.FromBase64String(salt), iterations))
            {
                return Convert.ToBase64String(pbkdf2.GetBytes(24));
            }
        }

        public async Task<CreateUserResult> CreateUserAsync(ApiUser user)
        {
            var result = new CreateUserResult();

            var salt = GenerateSalt();
            var hashedPassword = HashedPassword(user.Password, salt);

            var (dataUser, error) = await _dataStorage.CreateUserAsync(user.Username, salt, hashedPassword);

            if (dataUser != null)
            {
                result.Success = true;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = error;
            }

            return result;
        }

        public async Task OnEmailReceivedAsync(string username, string from, IEnumerable<string> to, IReadOnlyDictionary<string, string> parameters, MimeMessage mimeMessage)
        {
            var receivedTimestamp = DateTime.UtcNow;
            _log.LogDebug($"message received: timestamp='{receivedTimestamp}'");

            var attachments = mimeMessage.Attachments.Select(x => new DataAttachment { Filename = "debugme", Content = new byte[0] }).ToArray();

            await _dataStorage.CreateEmailAsync(username, receivedTimestamp, from, to.ToArray(), mimeMessage.Subject, mimeMessage.TextBody, attachments);
        }

        public async Task<AuthenticateUserResult> AuthenticateUserAsync(string username, string password)
        {
            var user = await _dataStorage.ReadUserAsync(username);

            if (user == null)
                return new AuthenticateUserResult
                {
                    Success = false,
                    ErrorMessage = "User not found"
                };

            var salt = user.Salt;
            var hashedPassword = HashedPassword(password, salt);

            if (hashedPassword == user.HashedPassword)
            {
                return new AuthenticateUserResult
                {
                    Success = true,
                    ErrorMessage = string.Empty
                };
            }

            return new AuthenticateUserResult
            {
                Success = false,
                ErrorMessage = "Incorrect password"
            };
        }

        public async Task<ListEmailResult> ReadEmailsAsync(string username, string password, int skip, int take)
        {
            var authResult = await AuthenticateUserAsync(username, password);
            if (!authResult.Success)
            {
                return new ListEmailResult
                {
                    Success = false,
                    ErrorMessage = "Authentication failure"
                };
            }

            var emails = await _dataStorage.ReadEmailsAsync(username, skip, take);
            return new ListEmailResult
            {
                Success = true,
                Emails = emails.Select(e => new ApiEmail
                {
                    Subject = e.Subject,
                    TextBody = e.TextBody,
                    From = e.From,
                    To = e.To.Split("; "),
                    EmailId = e.EmailId,
                    Attachments = e.Attachments?.Select(a => new ApiAttachment
                    {
                        AttachmentId = a.AttachmentId,
                        Content = a.Content,
                        Filename = a.Filename
                    }).ToArray(),
                }).ToArray()
            };
        }
    }
}
