using Broker.Services.Interfaces;
using Grpc.Core;
using GrpcAgent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Broker.Services
{
    public class SenderWorker : IHostedService
    {
        private Timer _timer;
        private const int TimeToWait = 2000;
        private readonly IMessageStorageService _messageStorage;
        private readonly IConnectionStorageService _connectionStorage;

        public SenderWorker(IServiceScopeFactory serviceScopeFactory)
        {
            using (var scope = serviceScopeFactory.CreateScope())
            {
                _messageStorage = scope.ServiceProvider.GetRequiredService<IMessageStorageService>();
                _connectionStorage = scope.ServiceProvider.GetRequiredService<IConnectionStorageService>();
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoSendWork, null, 0, TimeToWait);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSendWork(object state)
        {
            // Process only one message per timer tick to avoid duplicates
            if (!_messageStorage.IsEmpty())
            {
                var message = _messageStorage.GetNext();

                if (message != null)
                {
                    Console.WriteLine($"Processing message: {message.Content} for topic: {message.Topic}");
                    
                    var connections = _connectionStorage.GetConnectionsByTopic(message.Topic);
                    Console.WriteLine($"Found {connections.Count} connections for topic: {message.Topic}");

                    foreach(var connection in connections)
                    {
                        var client = new Notifier.NotifierClient(connection.Channel);
                        var request = new NotifyRequest() { Content = message.Content, Topic = message.Topic };

                        try
                        {
                            var reply = client.Notify(request);
                            Console.WriteLine($"Notified subscriber {connection.Address} with {message.Content} for topic {message.Topic}. " +
                                $"Response: {reply.IsSuccess} ");
                        }
                        catch (RpcException rpcException)
                        {
                            if (rpcException.StatusCode == StatusCode.Internal)
                            {
                                _connectionStorage.Remove(connection.Address);
                            }

                            Console.WriteLine($"RPC Error notyfing subscriber {connection.Address}. {rpcException.Message}");
                        }
                        catch (Exception exception)
                        {

                            Console.WriteLine($"Error notifying subscriber {connection.Address}. {exception.Message}");
                        }
                    }
                }
            }
        }
    }
}
