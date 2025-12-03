using Common;
using Grpc.Net.Client;
using GrpcAgent;
using System;
using System.Threading.Tasks;

namespace Sender
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Publisher");

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var channel = GrpcChannel.ForAddress(EndPointsConstants.BrokerAddress);
            var client = new Publisher.PublisherClient(channel);

            while (true)
            {
                Console.Write("Enter the topic: ");
                var topic = Console.ReadLine().ToLower();

                Console.Write("Enter content: ");
                var content = Console.ReadLine();

                var request = new PublishRequest { Topic = topic, Content = content };

                try
                {
                    var replay = await client.PublishMessageAsync(request);
                    Console.WriteLine($"Publish reply: {replay.IsSuccess}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error publishing the message: {e.Message}");
                }
            }
        }
    }
}
