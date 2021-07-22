using Commands;
using Components;
using HECSFramework.Core;
using HECSFramework.Network;
using HECSFramework.Server;
using LiteNetLib;
using MessagePack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Systems
{
    public class ServerNetworkSystem : BaseSystem, IServerNetworkSystem, IUpdatable, IAfterEntityInit,
        IReactGlobalCommand<InitNetworkSystemCommand>,
        IReactGlobalCommand<StopServerCommand>
    {
        private enum ServerNetworkSystemState { Default, Sync }
        private string connectionRequestKey = "Test";

        private ConnectionsHolderComponent connections;
        private IDataSenderSystem dataSenderSystem;
        private IDataProcessor dataProcessor = new HECSDataProcessor();

        private ConcurrentDictionary<int, List<ResolverDataContainer>> pullResolvers = new ConcurrentDictionary<int, List<ResolverDataContainer>>();
        private bool[] lockedLists = new bool[64];

        private ServerNetworkSystemState state;
        public static object Lock = new ActorContainerID();

        private int clientConnectCommandID = IndexGenerator.GetIndexForType(typeof(ClientConnectCommand));
        private AppVersionComponent applVersionComponent;

        public override void InitSystem()
        {
            Owner.TryGetHecsComponent(out applVersionComponent);
            connections = Owner.GetHECSComponent<ConnectionsHolderComponent>();

            for (int i = 0; i < lockedLists.Length; i++)
                pullResolvers.TryAdd(i, new List<ResolverDataContainer>(256));
        }

        public void AfterEntityInit()
        {
            Owner.TryGetSystem(out dataSenderSystem);
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
            var bytes = netPacketReader.GetRemainingBytes();
            var message = MessagePackSerializer.Deserialize<ResolverDataContainer>(bytes);

            if (message.TypeHashCode == clientConnectCommandID)
            {
                var connect = MessagePack.MessagePackSerializer.Deserialize<ClientConnectCommand>(message.Data);
                var id = peer.EndPoint.GetHashCode();
                var clientGuid = connect.Client;

                if (connect.Version != applVersionComponent.Version)
                {
                    dataSenderSystem.SendCommand(peer, Guid.Empty, new DisconnectCommand { Reason = "Update your client to actual version" });
                    peer.Disconnect();
                    return;
                }

                if (connections.ClientConnectionsID.ContainsKey(id))
                {
                    if (this.connections.TryGetClientByConnectionID(id, out var clientByID))
                        this.connections.Owner.Command(new RemoveClientCommand { ClientGuidToRemove = clientByID });
                }

                connections.ClientConnectionsID.TryAdd(id, peer);
                connections.ClientConnectionsGUID.TryAdd(clientGuid, peer);
                connections.ClientConnectionsTimes.TryAdd(clientGuid, DateTime.Now);;

                //todo подключить логи/статистику
                //if (Config.Instance.StatisticsData.ExtendedStatisticsEnabled)
                //    peer.NetManager.EnableStatistics = true;
            }

            try
            {
                dataProcessor.Process(message);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
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

            Owner.Command(new SyncComponentsCommand());
        }
       
        public void CommandGlobalReact(InitNetworkSystemCommand command)
        {
            if (!string.IsNullOrEmpty(command.Key))
            {
                connectionRequestKey = command.Key;
            }

            connections = Owner.GetHECSComponent<ConnectionsHolderComponent>();
            EventBasedNetListener listener = new EventBasedNetListener();
            connections.NetManager = new NetManager(listener);
            connections.NetManager.UpdateTime = Config.Instance.ServerTickMilliseconds;
            connections.NetManager.DisconnectTimeout = 30000;
            connections.NetManager.ChannelsCount = 64;
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
            Debug.Log("Received stop server command.");
            foreach (var kvp in Owner.GetConnectionsHolderComponent().ClientConnectionsGUID)
                kvp.Value.Disconnect();
            Environment.Exit(0);
        }
    }

    internal interface IServerNetworkSystem : ISystem
    {
    }
}
