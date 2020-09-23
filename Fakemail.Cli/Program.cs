using System;
using System.Threading.Tasks;

using Grpc.Net.Client;
using Fakemail.Grpc;

namespace Fakemail.Cli
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new PublicApi.PublicApiClient(channel);
            var request = new GetMailboxStatusRequest { Mailbox = "e67f0c7f-4947-4be3-abe4-a1f1b3f3aef7@fakemail.stream" };
            var reply = await client.GetMailboxStatusAsync(request);

            if (reply.ReplyMetadata.Success)
            {
                Console.WriteLine($"Mailbox {request.Mailbox} {(reply.Exists ? "exists" : "does not exist")}!");
            }
            else
            {
                Console.WriteLine(reply.ReplyMetadata.StatusMessage);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
