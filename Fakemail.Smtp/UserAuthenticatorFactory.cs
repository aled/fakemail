using SmtpServer;
using SmtpServer.Authentication;

namespace Fakemail.Smtp
{
    public class UserAuthenticatorFactory : IUserAuthenticatorFactory
    {
        public IUserAuthenticator _userAuthenticator;

        public UserAuthenticatorFactory(IUserAuthenticator userAuthenticator)
        {
            _userAuthenticator = userAuthenticator;
        }

        public IUserAuthenticator CreateInstance(ISessionContext context)
        {
            return _userAuthenticator;
        }
    }
}
