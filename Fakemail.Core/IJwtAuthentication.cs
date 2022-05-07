namespace Fakemail.Core
{
    public interface IJwtAuthentication
    {
        string GetAuthenticationToken(string username, bool isAdmin);
    }
}
