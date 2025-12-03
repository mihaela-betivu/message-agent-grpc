using Broker.Models;
using Broker.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Broker.Services
{
    public class MessageStorageService : IMessageStorageService
    {
        private readonly ConcurrentQueue<Message> _messages;
        private readonly List<Message> _messageHistory;
        private readonly object _historyLocker;

        public MessageStorageService()
        {
            _messages = new ConcurrentQueue<Message>();
            _messageHistory = new List<Message>();
            _historyLocker = new object();
        }

        public void Add(Message message)
        {
            _messages.Enqueue(message);
            
            // Keep message in history
            lock (_historyLocker)
            {
                _messageHistory.Add(message);
            }
        }

        public Message GetNext()
        {
            Message message;
            _messages.TryDequeue(out message);

            return message;
        }

        public bool IsEmpty()
        {
            return _messages.IsEmpty;
        }

        public IList<Message> GetMessagesByTopic(string topic)
        {
            lock (_historyLocker)
            {
                return _messageHistory.Where(m => m.Topic == topic).ToList();
            }
        }

        public IList<Message> GetRecentMessagesByTopic(string topic, int count = 10)
        {
            lock (_historyLocker)
            {
                var topicMessages = _messageHistory
                    .Where(m => m.Topic == topic)
                    .OrderBy(m => m.Timestamp)  // Sort by timestamp ascending (oldest first)
                    .ToList();
                
                // If we have more messages than requested, take the last N
                if (topicMessages.Count > count)
                {
                    return topicMessages.Skip(topicMessages.Count - count).ToList();
                }
                
                return topicMessages;
            }
        }
    }
}
