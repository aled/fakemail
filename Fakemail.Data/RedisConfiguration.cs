namespace Fakemail.Data
{
    public class RedisConfiguration : IRedisConfiguration
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Password { get; set; }
        public int DatabaseNumber { get; set; }
    }
}
