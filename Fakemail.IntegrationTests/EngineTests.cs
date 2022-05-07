using System.Threading.Tasks;

using Xunit;
using FluentAssertions;

using MimeKit;

using Fakemail.ApiModels;
using Fakemail.Core;

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

            var response = await _fixture.Engine.CreateUserAsync(
                new CreateUserRequest {
                    Username = username,
                    Password = password
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

            var response = await _fixture.Engine.CreateUserAsync(
                new CreateUserRequest
                {
                    Username = username,
                    Password = password
                }
            );

            response.Should().NotBeNull();
            response.Success.Should().BeTrue();

            response = await _fixture.Engine.CreateUserAsync(
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

            var response = await _fixture.Engine.CreateUserAsync(request);

            response.Should().NotBeNull();
            response.Success.Should().BeTrue();

            var authRequest = new GetTokenRequest
            {
                Username = request.Username,
                Password = "WrongPassword!!!"
            };

            var authResponse = await _fixture.Engine.GetTokenAsync(authRequest);

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

            var response = await _fixture.Engine.CreateUserAsync(request);

            response.Should().NotBeNull();
            response.Success.Should().BeTrue();

            var authRequest = new GetTokenRequest
            {
                Username = request.Username,
                Password = request.Password
            };

            var authResponse = await _fixture.Engine.GetTokenAsync(authRequest);

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

            var response = await _fixture.Engine.CreateUserAsync(request);

            response.Should().NotBeNull();
            response.Success.Should().BeTrue();

            var authRequest = new GetTokenRequest
            {
                Username = "NoSuchUser!!!",
                Password = request.Password
            };

            var authResponse = await _fixture.Engine.GetTokenAsync(authRequest);

            authResponse.Success.Should().Be(false);
            authResponse.Token.Should().BeNull();
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