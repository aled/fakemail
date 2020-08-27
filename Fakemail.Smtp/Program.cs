using System.Threading;
using System.Threading.Tasks;

using Fakemail.Data;

namespace Fakemail.Smtp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var redisConfiguration = new RedisConfiguration
            {
                Host = "localhost",
                Port = 6379,
                Password = "Password1!",
                DatabaseNumber = 1
            };

            await new Server().RunAsync(redisConfiguration, CancellationToken.None);
        }
    }
}
