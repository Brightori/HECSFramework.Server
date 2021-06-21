using HECSFramework.Core;
using LiteNetLib;
using System;
using System.Collections.Concurrent;

namespace Components
{
    internal class ConnectionsHolderComponent : BaseComponent, IConnectionsHolderComponent
    {
        public ConcurrentDictionary<Guid, DateTime> ClientConnectionsTimes { get; } = new ConcurrentDictionary<Guid, DateTime>();
        public ConcurrentDictionary<Guid, NetPeer> ClientConnectionsGUID { get; } = new ConcurrentDictionary<Guid, NetPeer>();
        public ConcurrentDictionary<int, NetPeer> ClientConnectionsID { get; } = new ConcurrentDictionary<int, NetPeer>();
        public ConcurrentDictionary<int, ConcurrentDictionary<int, NetPeer>> WorldToPeerClients { get; } = new ConcurrentDictionary<int, ConcurrentDictionary<int, NetPeer>>();
        public ConcurrentDictionary<Guid, int> EntityToWorldConnections { get; } = new ConcurrentDictionary<Guid, int>();
        public NetManager NetManager { get; set; }

        public bool TryGetClientByConnectionID(int connectionID, out Guid clientGuid)
        {
            foreach (var connectionInfo in ClientConnectionsGUID)
            {
                if (connectionInfo.Value.EndPoint.GetHashCode() == connectionID)
                {
                    clientGuid = connectionInfo.Key;
                    return true;
                }
            }

            clientGuid = default;
            return false;
        }
    }

    public interface IConnectionsHolderComponent : IComponent
    {
        ConcurrentDictionary<Guid, DateTime> ClientConnectionsTimes { get; }
        ConcurrentDictionary<Guid, NetPeer> ClientConnectionsGUID { get; }
        ConcurrentDictionary<int, NetPeer> ClientConnectionsID { get; }
        ConcurrentDictionary<Guid, int> EntityToWorldConnections { get; }
        ConcurrentDictionary<int, ConcurrentDictionary<int, NetPeer>> WorldToPeerClients { get; }
        bool TryGetClientByConnectionID(int connectionID, out Guid clientGuid);
        NetManager NetManager { get; set; }
    }
}
