using SmtpServer;
using SmtpServer.Authentication;

namespace Fakemail.Smtp
{
    public class FakemailUserAuthenticatorFactory : IUserAuthenticatorFactory
    {
        public IUserAuthenticator _userAuthenticator;

        public FakemailUserAuthenticatorFactory(IUserAuthenticator userAuthenticator)
        {
            _userAuthenticator = userAuthenticator;
        }

        public IUserAuthenticator CreateInstance(ISessionContext context)
        {
            return _userAuthenticator;
        }
    }
}
