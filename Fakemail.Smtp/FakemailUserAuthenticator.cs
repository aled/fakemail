using Fakemail.ApiModels;
using Fakemail.Core;

using Microsoft.Extensions.Logging;

using SmtpServer;
using SmtpServer.Authentication;

using System.Threading;
using System.Threading.Tasks;

namespace Fakemail.Smtp
{
    public class FakemailUserAuthenticator : IUserAuthenticator
    {
        private ILogger<FakemailUserAuthenticator> _log;
        private IEngine _engine;

        public FakemailUserAuthenticator(ILogger<FakemailUserAuthenticator> log, IEngine engine)
        {
            _log = log;
            _engine = engine;
        }

        public async Task<bool> AuthenticateAsync(ISessionContext context, string username, string password, CancellationToken cancellationToken)
        {
            var user = new User
            {
                Username = username,
                Password = password
            };

            return (await _engine.AuthenticateUserAsync(user)).Success;
        }
    }
}
