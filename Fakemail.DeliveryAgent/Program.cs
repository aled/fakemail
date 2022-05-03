using System;

using MimeKit;

// Read a mime-encoded email from stdin, and write it to a database
namespace Fakemail.DeliveryAgent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new Program().Deliver(Console.OpenStandardInput());
        }

        public void Deliver(Stream messageStream)
        {


            
        }
    }
}
