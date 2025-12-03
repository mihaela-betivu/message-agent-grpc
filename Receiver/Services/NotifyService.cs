using Grpc.Core;
using GrpcAgent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Receiver.Services
{
    public class NotifyService : Notifier.NotifierBase
    {
        public override Task<NotifyReply> Notify(NotifyRequest request, ServerCallContext context)
        {
            Console.WriteLine($"Received: {request.Content} for topic: {request.Topic}");

            return Task.FromResult(new NotifyReply() {IsSuccess = true });
        }
    }
}
