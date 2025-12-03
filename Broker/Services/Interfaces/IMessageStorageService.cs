using Broker.Models;
using System.Collections.Generic;

namespace Broker.Services.Interfaces
{
    public interface IMessageStorageService
    {
        void Add(Message message);

        Message GetNext();

        bool IsEmpty();

        IList<Message> GetMessagesByTopic(string topic);

        IList<Message> GetRecentMessagesByTopic(string topic, int count = 10);
    }
}
