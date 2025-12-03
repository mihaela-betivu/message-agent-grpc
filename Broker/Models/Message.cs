using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Broker.Models
{
    public class Message
    {
        public Message(string topic, string content)
        {
            Topic = topic;
            Content = content;
            Timestamp = DateTime.UtcNow;
        }

        public string Topic { get; }
        public string Content { get; }
        public DateTime Timestamp { get; }
    }
}
