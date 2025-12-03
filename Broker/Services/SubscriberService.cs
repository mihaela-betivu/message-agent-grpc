using Broker.Models;
using Broker.Services.Interfaces;
using Grpc.Core;
using GrpcAgent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Broker.Services
{
    public class SubscriberService : Subscriber.SubscriberBase
    {
        private readonly IConnectionStorageService _connectionStorage;
        private readonly IMessageStorageService _messageStorage;

        public SubscriberService(IConnectionStorageService connectionStorage, IMessageStorageService messageStorage)
        {
            _connectionStorage = connectionStorage;
            _messageStorage = messageStorage;
        }

        public override async Task<SubscribeReply> Subscribe(SubscribeRequest request, ServerCallContext context)
        {
            Console.WriteLine($"New client is trying to subscribe: {request.Address} {request.Topic}");

            try
            {
                var connection = new Connection(request.Address, request.Topic);
                _connectionStorage.Add(connection);

                // Send historical messages to the new subscriber
                await SendHistoricalMessages(connection, request.Topic);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not add the new connection {request.Address} {request.Topic} {e.Message}");
            }

            return new SubscribeReply()
            {
                IsSuccess = true
            };
        }

        private async Task SendHistoricalMessages(Connection connection, string topic)
        {
            try
            {
                // Get recent messages for this topic (last 10 messages)
                var historicalMessages = _messageStorage.GetRecentMessagesByTopic(topic, 10);
                
                if (historicalMessages.Any())
                {
                    Console.WriteLine($"Sending {historicalMessages.Count} historical messages to {connection.Address} for topic {topic}");
                    
                    // Debug: Show the order of messages being sent
                    Console.WriteLine($"Historical messages order for topic {topic}:");
                    for (int debugIndex = 0; debugIndex < historicalMessages.Count; debugIndex++)
                    {
                        var debugMessage = historicalMessages[debugIndex];
                        Console.WriteLine($"  {debugIndex + 1}. {debugMessage.Timestamp:mm:ss} - {debugMessage.Content}");
                    }
                    
                    var client = new Notifier.NotifierClient(connection.Channel);
                    
                    for (int i = 0; i < historicalMessages.Count; i++)
                    {
                        var message = historicalMessages[i];
                        try
                        {
                            var request = new NotifyRequest() 
                            { 
                                Content = $"[HISTORICAL] {message.Content} (sent at {message.Timestamp:mm:ss})",
                                Topic = message.Topic
                            };
                            
                            var reply = await client.NotifyAsync(request);
                            Console.WriteLine($"Sent historical message {i + 1}/{historicalMessages.Count} to {connection.Address}: {message.Content} for topic {message.Topic} at {message.Timestamp:mm:ss}");
                            
                            // Small delay between messages to avoid overwhelming the client
                            await Task.Delay(100);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Error sending historical message {i + 1} to {connection.Address}: {e.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"No historical messages found for topic {topic}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error sending historical messages to {connection.Address}: {e.Message}");
            }
        }
    }
}
