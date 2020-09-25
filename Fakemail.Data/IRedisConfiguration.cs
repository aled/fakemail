namespace Fakemail.Data
{
    public interface IRedisConfiguration
    {
        string Host { get; set; }
        string Password { get; set; }
        int Port { get; set; }
        int DatabaseNumber { get; set; }
    }
}
