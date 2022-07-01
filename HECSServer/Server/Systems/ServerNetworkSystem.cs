using System.Collections.Concurrent;
using Commands;
using Components;
using HECSFramework.Core;
using HECSFramework.Network;
using HECSFramework.Server;
using LiteNetLib;
using MessagePack;

namespace Systems
{
    public class ServerNetworkSystem : BaseSystem, IServerNetworkSystem
    {
        private enum ServerNetworkSystemState { Default, Sync }
        private string connectionRequestKey = "Test";

        private ConnectionsHolderComponent connections;
        private DataSenderSystem dataSenderSystem;
        private IDataProcessor dataProcessor = new HECSDataProcessor();

        private ConcurrentDictionary<int, List<ResolverDataContainer>> pullResolvers = new ConcurrentDictionary<int, List<ResolverDataContainer>>();
        private bool[] lockedLists = new bool[64];

        private ServerNetworkSystemState state;

        private int clientConnectCommandID = IndexGenerator.GetIndexForType(typeof(ClientConnectCommand));

        public override void InitSystem()
        {
        }

        public virtual void GlobalStart()
        {
            connections = Owner.World.GetSingleComponent<ConnectionsHolderComponent>();

            for (int i = 0; i < lockedLists.Length; i++)
                pullResolvers.TryAdd(i, new List<ResolverDataContainer>(256));

            dataSenderSystem = Owner.World.GetSingleSystem<DataSenderSystem>();
        }

        private void Listener_ConnectionRequestEvent(ConnectionRequest request)
        {
            request.AcceptIfKey(connectionRequestKey);
        }

        private void ListenerOnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectinfo)
        {
            if (connections.TryGetClientByConnectionID(peer.EndPoint.GetHashCode(), out var clientGuid))
            {
                Owner.Command(new RemoveClientCommand { ClientGuidToRemove = clientGuid });
            }
        }

        private void ListenerOnPeerConnectedEvent(NetPeer peer)
        {
            //connectionsHolderComponent.ClientConnectionsID.Add(peer.Id, peer);
        }

        private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader netPacketReader, DeliveryMethod deliveryMethod)
        {
            try
            {
                var bytes = netPacketReader.GetRemainingBytes();
                var message = MessagePackSerializer.Deserialize<ResolverDataContainer>(bytes);

                CheckIfNewConnection(peer, message);

                if (!connections.PeerToWorldConnections.TryGetValue(peer, out var world))
                    return;

                dataProcessor.Process(message, world);
                EntityManager.Command(new RawStatisticsCommand { ResolverDataContainer = message });
            }
            catch (Exception e)
            {
                HECSDebug.LogError(e.ToString());
            }
        }

        private void CheckIfNewConnection(NetPeer peer, ResolverDataContainer message)
        {
            if (message.TypeHashCode != clientConnectCommandID) return;

            var connect = MessagePackSerializer.Deserialize<ClientConnectCommand>(message.Data);
            var id = peer.EndPoint.GetHashCode();
            var clientGuid = connect.Client;

            if (connections.ClientConnectionsID.ContainsKey(id) && connections.TryGetClientByConnectionID(id, out var clientByID))
                connections.Owner.Command(new RemoveClientCommand { ClientGuidToRemove = clientByID });

            connections.ClientConnectionsID.TryAdd(id, peer);
            connections.ClientConnectionsGUID.TryAdd(clientGuid, peer);
            connections.ClientConnectionsTimes.TryAdd(clientGuid, DateTime.Now);
            connections.PeerToWorldConnections.TryAdd(peer, EntityManager.Worlds.Data[connect.RoomWorld]);
        }

        public void UpdateLocal()
        {
            switch (state)
            {
                case ServerNetworkSystemState.Default:
                    break;
                case ServerNetworkSystemState.Sync:
                    connections.NetManager.PollEvents();
                    break;
            }

            EntityManager.Command(new SyncComponentsCommand(), -1);
        }

        public void CommandGlobalReact(InitNetworkSystemCommand command)
        {
            if (!string.IsNullOrEmpty(command.Key))
            {
                connectionRequestKey = command.Key;
            }

            connections = EntityManager.GetSingleComponent<ConnectionsHolderComponent>();
            EventBasedNetListener listener = new EventBasedNetListener();
            connections.NetManager = new NetManager(listener);
            connections.NetManager.UpdateTime = Config.Instance.ServerTickMilliseconds;
            connections.NetManager.DisconnectTimeout = 30000;
            connections.NetManager.ChannelsCount = 64;
            if (Config.Instance.ExtendedStatisticsEnabled)
                connections.NetManager.EnableStatistics = true;
            connections.NetManager.Start(command.Port);

            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
            listener.PeerConnectedEvent += ListenerOnPeerConnectedEvent;
            listener.PeerDisconnectedEvent += ListenerOnPeerDisconnectedEvent;
            listener.ConnectionRequestEvent += Listener_ConnectionRequestEvent;

            connections.NetManager.UnsyncedEvents = true;
            state = ServerNetworkSystemState.Sync;
        }

        public void CommandGlobalReact(StopServerCommand command)
        {
            HECSDebug.Log("Received stop server command.");
            foreach (var kvp in EntityManager.GetSingleComponent<ConnectionsHolderComponent>().ClientConnectionsGUID)
                kvp.Value.Disconnect();
            Environment.Exit(0);
        }
    }

    internal interface IServerNetworkSystem : ISystem, IUpdatable,
        IReactGlobalCommand<InitNetworkSystemCommand>, IGlobalStart,
        IReactGlobalCommand<StopServerCommand>
    {
    }
}
