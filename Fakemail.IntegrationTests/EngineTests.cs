using System.Threading.Tasks;

using Fakemail.Core;

using FluentAssertions;

using MimeKit;

using Xunit;

namespace Fakemail.IntegrationTests
{

    public partial class EngineTests : IClassFixture<EngineFixture>
    {
        EngineFixture _fixture;

        public EngineTests(EngineFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CreateUser()
        {
            var username = Utils.CreateId();
            var password = Utils.CreateId();

            var result = await _fixture.Engine.CreateUserAsync(
                new ApiModels.User {
                    Username = username,
                    Password = password
                }
            );

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task CreateUser_WillNotCreateDuplicate()
        {
            var username = Utils.CreateId();
            var password = Utils.CreateId();

            var result = await _fixture.Engine.CreateUserAsync(
                new ApiModels.User
                {
                    Username = username,
                    Password = password
                }
            );

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            result = await _fixture.Engine.CreateUserAsync(
               new ApiModels.User
               {
                   Username = username,
                   Password = password
               }
           );

            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("User already exists");
        }

        [Fact]
        public async Task AuthenticateUser_WrongPassword()
        {
           var user = new ApiModels.User
            {
                Username = Utils.CreateId(),
                Password = Utils.CreateId()
            };

            var result = await _fixture.Engine.CreateUserAsync(user);

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            user.Password = "WrongPassword!!!";
            var auth = await _fixture.Engine.AuthenticateUserAsync(user);

            auth.Success.Should().Be(false);
        }

        [Fact]
        public async Task AuthenticateUser_CorrectPassword()
        {
            var user = new ApiModels.User
            {
                Username = Utils.CreateId(),
                Password = Utils.CreateId()
            };

            var result = await _fixture.Engine.CreateUserAsync(user);

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            var auth = await _fixture.Engine.AuthenticateUserAsync(user);

            auth.Success.Should().Be(true);
        }

        [Fact]
        public async Task AuthenticateUser_NoSuchUser()
        {
            var user = new ApiModels.User
            {
                Username = Utils.CreateId(),
                Password = Utils.CreateId()
            };

            var result = await _fixture.Engine.CreateUserAsync(user);

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            user.Username = "NoSuchUser!!!";
            var auth = await _fixture.Engine.AuthenticateUserAsync(user);

            auth.Success.Should().Be(false);
        }

        private static MimeMessage GenerateEmail()
        {
            var from = new InternetAddressList(new InternetAddress[] { new MailboxAddress("Fred Flintstone", "fred@flintstone.com") });
            var to = new InternetAddressList(new InternetAddress[] { new MailboxAddress("Barney Rubble", "barney@rubble.com") });
            var body = new TextPart("plain")
            {
                Text = "... dabba doo"
            };
            return new MimeMessage(from, to, "Yabba...", body);
        }
    }
}