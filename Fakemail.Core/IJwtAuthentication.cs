using System;

namespace Fakemail.Core
{
    public interface IJwtAuthentication
    {
        string GetAuthenticationToken(Guid userId, bool isAdmin);
    }
}
