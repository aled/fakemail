using Fakemail.Core;

using Microsoft.Extensions.Logging;

using SmtpServer;
using SmtpServer.Authentication;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fakemail.Smtp
{
    public class UserAuthenticator : IUserAuthenticator
    {
        private ILogger<UserAuthenticator> _log;
        private IEngine _engine;

        public UserAuthenticator(ILogger<UserAuthenticator> log, IEngine engine)
        {
            _log = log;
            _engine = engine;
        }

        public async Task<bool> AuthenticateAsync(ISessionContext context, string user, string password, CancellationToken cancellationToken)
        {
            return (await _engine.AuthenticateUserAsync(user, password)).Success;
        }
    }
}
