using Broker.Models;
using System.Collections.Generic;

namespace Broker.Services.Interfaces
{
    public interface IConnectionStorageService
    {
        void Add(Connection connection);

        void Remove(string address);

        IList<Connection> GetConnectionsByTopic(string topic);

        int GetConnectionCount();

        void LogConnections();
    }
}
