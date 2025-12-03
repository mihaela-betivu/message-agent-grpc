using Common;
using Grpc.Net.Client;
using GrpcAgent;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Receiver
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Subscriber");
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var host = WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls(EndPointsConstants.SubscriberAddress)
                .Build();
            host.Start();

            Subscribe(host);

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        }

        private static void Subscribe(IWebHost host)
        {
            Console.WriteLine($"Attempting to connect to Broker at: {EndPointsConstants.BrokerAddress}");
            
            // Simple gRPC channel configuration
            var channel = GrpcChannel.ForAddress(EndPointsConstants.BrokerAddress);
            var client = new Subscriber.SubscriberClient(channel);

            var address = host.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First();
            Console.WriteLine($"Subscriber is listening at: {address}");

            Console.WriteLine("Enter the topic: \n");
            var topic = Console.ReadLine().ToLower();
            
            Console.WriteLine($"Subscriber topic: {topic}");

            var request = new SubscribeRequest() { Topic = topic, Address = address};

            try
            {
                var reply = client.Subscribe(request);
                Console.WriteLine($"Subsribe reply: {reply.IsSuccess}");
            }
            catch (Exception e)
            {

                Console.WriteLine($"Error subscribing: {e.Message}");
            }
        }
    }
}
