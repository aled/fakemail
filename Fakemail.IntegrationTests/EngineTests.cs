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


        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public async Task CreateUser_WithTooShortUsername(int length)
        {
            var username = new string('a', length);
            var password = Utils.CreateId();

            var response = await _fixture.Engine.CreateUserAsync(
                new CreateUserRequest
                {
                    Username = username,
                    Password = password
                }
            );

            response.Should().NotBeNull();
            response.Success.Should().BeFalse();
            response.Password.Should().BeNull();
            response.ErrorMessage.Should().Be("Username length must be at least 6 characters");
        }

        [Fact]
        public async Task CreateUser_WithTooLongUsername()
        {
            var username = new string('a', 31); 
            var password = Utils.CreateId();

            var response = await _fixture.Engine.CreateUserAsync(
                new CreateUserRequest
                {
                    Username = username,
                    Password = password
                }
            );

            response.Should().NotBeNull();
            response.Success.Should().BeFalse();
            response.Password.Should().BeNull();
            response.ErrorMessage.Should().Be("Username length must not be greater than 30 characters");
        }


        [Theory]
        [InlineData(0)]
        [InlineData(9)]
        public async Task CreateUser_WithTooShortPassword(int length)
        {
            var username = Utils.CreateId();
            var password = new string('*', length);

            var response = await _fixture.Engine.CreateUserAsync(
                new CreateUserRequest {
                    Username = username,
                    Password = password
                }
            );

            response.Should().NotBeNull();
            response.Success.Should().BeFalse();
            response.Password.Should().BeNull();
            response.ErrorMessage.Should().Be("Password length must be at least 10 characters");
        }

        [Fact]
        public async Task CreateUser_WithTooLongPassword()
        {
            var username = Utils.CreateId();

            var response = await _fixture.Engine.CreateUserAsync(
                new CreateUserRequest
                {
                    Username = username,
                    Password = new string('*', 41)
                }
            );

            response.Should().NotBeNull();
            response.Success.Should().BeFalse();
            response.Password.Should().BeNull();
            response.ErrorMessage.Should().Be("Password length must not be greater than 40 characters");
        }

        [Theory]
        [InlineData(10)]
        [InlineData(39)]
        public async Task CreateUser_WithAdequatelySizedUsernameAndPassword(int length)
        {
            var username = Utils.CreateId();
            var password = (Utils.CreateId() + Utils.CreateId()).Substring(1, length);

            var response = await _fixture.Engine.CreateUserAsync(
                new CreateUserRequest
                {
                    Username = username,
                    Password = password
                }
            );

            response.Should().NotBeNull();
            response.Success.Should().BeTrue();
            response.Password.Should().BeNull();
        }

        [Fact]
        public async Task CreateUser_WithPwnedPassword()
        {
            var username = Utils.CreateId();

            var response = await _fixture.Engine.CreateUserAsync(
                new CreateUserRequest
                {
                    Username = username,
                    Password = EngineFixture.ExamplePwnedPassword // the password is asdfasdfasdf
                }
            );

            response.Should().NotBeNull();
            response.Success.Should().BeFalse();
            response.Password.Should().BeNull();
            response.ErrorMessage.Should().Be("Password was found in HaveIBeenPwned");
        }

        [Fact]
        public async Task CreateUser_WithNullPassword()
        {
            var username = Utils.CreateId();

            var response = await _fixture.Engine.CreateUserAsync(
                new CreateUserRequest
                {
                    Username = username,
                    Password = null
                }
            );

            response.Should().NotBeNull();
            response.Success.Should().BeTrue();
            response.Password.Should().NotBeNullOrEmpty();
            response.Password.Length.Should().Be(14);
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