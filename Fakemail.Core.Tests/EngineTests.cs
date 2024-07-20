using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Fakemail.ApiModels;

using FluentAssertions;

using Xunit;

namespace Fakemail.Core.Tests
{
    public partial class EngineTests(EngineFixture fixture) : IClassFixture<EngineFixture>
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public async Task CreateUser_WithTooShortUsername(int length)
        {
            var username = new string('a', length);
            var password = Utils.CreateId();

            var response = await fixture.Engine.CreateUserAsync(
                new CreateUserRequest
                {
                    Username = username,
                    Password = password
                }
            );

            response.Should().NotBeNull();
            response.Success.Should().BeFalse();
            response.ErrorMessage.Should().Be("Username length must be at least 6 characters");
        }

        [Fact]
        public async Task CreateUser_WithTooLongUsername()
        {
            var username = new string('a', 41);
            var password = Utils.CreateId();

            var response = await fixture.Engine.CreateUserAsync(
                new CreateUserRequest
                {
                    Username = username,
                    Password = password
                }
            );

            response.Should().NotBeNull();
            response.Success.Should().BeFalse();
            response.ErrorMessage.Should().Be("Username length must not be greater than 40 characters");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(9)]
        public async Task CreateUser_WithTooShortPassword(int length)
        {
            var username = Utils.CreateId();
            var password = new string('*', length);

            var response = await fixture.Engine.CreateUserAsync(
                new CreateUserRequest
                {
                    Username = username,
                    Password = password
                }
            );

            response.Should().NotBeNull();
            response.Success.Should().BeFalse();
            response.ErrorMessage.Should().Be("Password length must be at least 10 characters");
        }

        [Fact]
        public async Task CreateUser_WithTooLongPassword()
        {
            var username = Utils.CreateId();

            var response = await fixture.Engine.CreateUserAsync(
                new CreateUserRequest
                {
                    Username = username,
                    Password = new string('*', 41)
                }
            );

            response.Should().NotBeNull();
            response.Success.Should().BeFalse();
            response.ErrorMessage.Should().Be("Password length must not be greater than 40 characters");
        }

        [Theory]
        [InlineData(10)]
        [InlineData(39)]
        public async Task CreateUser_WithAdequatelySizedUsernameAndPassword(int length)
        {
            var username = Utils.CreateId();
            var password = (Utils.CreateId() + Utils.CreateId()).Substring(1, length);

            password.Length.Should().Be(length);

            var response = await fixture.Engine.CreateUserAsync(
                new CreateUserRequest
                {
                    Username = username,
                    Password = password
                }
            );

            response.Should().NotBeNull();
            response.Success.Should().BeTrue();
        }

        [Fact]
        public async Task CreateUser_WithPwnedPassword()
        {
            var username = Utils.CreateId();

            var response = await fixture.Engine.CreateUserAsync(
                new CreateUserRequest
                {
                    Username = username,
                    Password = EngineFixture.ExamplePwnedPassword  // the password is asdfasdfasdf
                }
            );

            response.Should().NotBeNull();
            response.Success.Should().BeFalse();
            response.ErrorMessage.Should().Be("Password was found in HaveIBeenPwned");
            response.UserId.Should().Be(null);
        }

        [Fact]
        public async Task CreateUser_WithNullPassword()
        {
            var username = Utils.CreateId();

            var response = await fixture.Engine.CreateUserAsync(
                new CreateUserRequest
                {
                    Username = username,
                    Password = null
                }
            );

            response.Should().NotBeNull();
            response.Success.Should().BeTrue();
        }

        [Fact]
        public async Task CreateUser_WillNotCreateDuplicate()
        {
            var username = Utils.CreateId();
            var password = Utils.CreateId();

            var response = await fixture.Engine.CreateUserAsync(
                new CreateUserRequest
                {
                    Username = username,
                    Password = password
                }
            );

            response.Should().NotBeNull();
            response.Success.Should().BeTrue();

            response = await fixture.Engine.CreateUserAsync(
               new CreateUserRequest
               {
                   Username = username,
                   Password = password
               }
           );

            response.Should().NotBeNull();
            response.Success.Should().BeFalse();
            response.ErrorMessage.Should().Be("User already exists");
        }

        [Fact]
        public async Task AuthenticateUser_WrongPassword()
        {
            var request = new CreateUserRequest
            {
                Username = Utils.CreateId(),
                Password = Utils.CreateId()
            };

            var response = await fixture.Engine.CreateUserAsync(request);

            response.Should().NotBeNull();
            response.Success.Should().BeTrue();

            var authRequest = new GetTokenRequest
            {
                UserId = response.UserId ?? Guid.Empty,
                Password = "WrongPassword!!!"
            };

            var authResponse = await fixture.Engine.GetTokenAsync(authRequest);

            authResponse.Success.Should().Be(false);
            authResponse.Token.Should().BeNull();
        }

        [Fact]
        public async Task AuthenticateUser_CorrectPassword()
        {
            var request = new CreateUserRequest
            {
                Username = Utils.CreateId(),
                Password = Utils.CreateId()
            };

            var response = await fixture.Engine.CreateUserAsync(request);

            response.Should().NotBeNull();
            response.Success.Should().BeTrue();

            var authRequest = new GetTokenRequest
            {
                UserId = response.UserId ?? Guid.Empty,
                Password = request.Password
            };

            var authResponse = await fixture.Engine.GetTokenAsync(authRequest);

            authResponse.Success.Should().Be(true);
            authResponse.Token.Should().NotBeNull();
        }

        [Fact]
        public async Task AuthenticateUser_NoSuchUser()
        {
            var request = new CreateUserRequest
            {
                Username = Utils.CreateId(),
                Password = Utils.CreateId()
            };

            var response = await fixture.Engine.CreateUserAsync(request);

            response.Should().NotBeNull();
            response.Success.Should().BeTrue();

            var authRequest = new GetTokenRequest
            {
                UserId = Guid.NewGuid(),
                Password = request.Password
            };

            var authResponse = await fixture.Engine.GetTokenAsync(authRequest);

            authResponse.Success.Should().Be(false);
            authResponse.Token.Should().BeNull();
        }

        private async Task CreateEmail(string smtpUsername, DateTimeOffset receivedTimestamp)
        {
            var strTime = receivedTimestamp.ToString("r").Replace("GMT", "+0000 (GMT)");

            var raw = "Return-Path: <From@From.example.com>\n" +
                "Delivered-To: To@example.stream\n" +
                "Received: from examplehost (static-123-234-12-23.example.co.uk [123.234.12.23])" +
                    $"\tby fakemail.stream (OpenSMTPD) with ESMTPSA id 22e6eb31 (TLSv1.2:ECDHE-RSA-AES256-GCM-SHA384:256:NO) auth=yes user={smtpUsername};" +
                    $"\t{strTime}\n" +
                "MIME-Version: 1.0\n" +
                "From: From@From.example.com\n" +
                "To: To@example.stream, To@example2.stream\n" +
                "Date: 30 Apr 2022 14:43:23 +0100\n" +
                "Subject: Subject\n" +
                "Content-Type: multipart/mixed;\n" +
                " boundary=--boundary_0_49d7d9ea-01d1-4f5c-91a5-19930730ea52\n" +
                "\n" +
                "\n" +
                "----boundary_0_49d7d9ea-01d1-4f5c-91a5-19930730ea52\n" +
                "Content-Type: text/plain; charset=us-ascii\n" +
                "Content-Transfer-Encoding: quoted-printable\n" +
                "\n" +
                "Body\n" +
                "----boundary_0_49d7d9ea-01d1-4f5c-91a5-19930730ea52\n" +
                "Content-Type: application/octet-stream; name=a.txt\n" +
                "Content-Transfer-Encoding: base64\n" +
                "Content-Disposition: attachment\n" +
                "\n" +
                "aGVsbG8=\n" +
                "----boundary_0_49d7d9ea-01d1-4f5c-91a5-19930730ea52--\n" +
                "\n";

            var response = await fixture.Engine.CreateEmailAsync(new MemoryStream(Encoding.UTF8.GetBytes(raw)));
            response.ErrorMessage.Should().BeNull();
        }

        private async Task<(Guid, string)> CreateUser()
        {
            var response = await fixture.Engine.CreateUserAsync(new CreateUserRequest());

            response.Should().NotBeNull();
            response.ErrorMessage.Should().BeNull();

            return (response.UserId!.Value, response.SmtpUsername);
        }

        [Fact]
        public async Task Cleanup()
        {
            var users = new (Guid id, string smtpUsername)[3];

            for (int i = 0; i < users.Length; i++)
            {
                users[i] = await CreateUser();
            }

            for (int i = 0; i < 4; i++)
            {
                fixture.TimeProvider.Advance(TimeSpan.FromSeconds(30));
                foreach (var (id, smtpUsername) in users)
                {
                    await CreateEmail(smtpUsername, fixture.TimeProvider.GetUtcNow());
                }
            }

            // each user has 4 emails (received at times now, now-30, now-60 and now-90)
            foreach (var user in users)
            {
                (await fixture.Engine.ListEmailsAsync(new ListEmailsRequest { UserId = user.id }, user.id))
                    .Emails.Count.Should().Be(4);
            }

            // Cleanup all emails over 90s old (one for each user)
            (await fixture.Engine.CleanupEmailsAsync(new CleanupEmailsRequest
            {
                MaxEmailAgeSeconds = 90,
                MaxEmailCount = 4
            }, CancellationToken.None)).TotalEmailsDeleted.Should().Be(3);

            foreach (var user in users)
            {
                (await fixture.Engine.ListEmailsAsync(new ListEmailsRequest { UserId = user.id }, user.id))
                    .Emails.Count.Should().Be(3);
            }

            // Cleanup oldest emails leaving 2 for each user
            (await fixture.Engine.CleanupEmailsAsync(new CleanupEmailsRequest
            {
                MaxEmailAgeSeconds = 99999,
                MaxEmailCount = 2
            }, CancellationToken.None)).TotalEmailsDeleted.Should().Be(3);

            foreach (var user in users)
            {
                (await fixture.Engine.ListEmailsAsync(new ListEmailsRequest { UserId = user.id }, user.id))
                    .Emails.Count.Should().Be(2);
            }
        }
    }
}