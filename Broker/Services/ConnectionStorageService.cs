using Broker.Models;
using Broker.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Broker.Services
{
    public class ConnectionStorageService : IConnectionStorageService
    {
        private readonly List<Connection> _connections;
        private readonly object _locker;

        public ConnectionStorageService()
        {
            _connections = new List<Connection>();
            _locker = new object();
        }

        public void Add(Connection connection)
        {
            lock (_locker)
            {
                // Check if connection already exists (same address and topic)
                var existingConnection = _connections.FirstOrDefault(c => 
                    c.Address == connection.Address && c.Topic == connection.Topic);
                
                if (existingConnection == null)
                {
                    _connections.Add(connection);
                    Console.WriteLine($"Added new connection: {connection.Address} for topic: {connection.Topic}");
                }
                else
                {
                    Console.WriteLine($"Connection already exists: {connection.Address} for topic: {connection.Topic}");
                }
            }
        }

        public IList<Connection> GetConnectionsByTopic(string topic)
        {
            lock (_locker)
            {
                var filteredConnections = _connections.Where(x => x.Topic == topic).ToList();
                return filteredConnections;
            }
        }

        public void Remove(string address)
        {
            lock (_locker)
            {
                _connections.RemoveAll(x => x.Address == address);
            }
        }

        public int GetConnectionCount()
        {
            lock (_locker)
            {
                return _connections.Count;
            }
        }

        public void LogConnections()
        {
            lock (_locker)
            {
                Console.WriteLine($"Total connections: {_connections.Count}");
                foreach (var conn in _connections)
                {
                    Console.WriteLine($"  - {conn.Address} (topic: {conn.Topic})");
                }
            }
        }
    }
}
