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

        public override void InitSystem()
        {
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
            dataProcessor.Process(message);
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

            ProcessNetWorkComponents();
        }

        private void ProcessNetWorkComponents()
        {
            var currentIndex = 0;
            List<ResolverDataContainer> currentList;
            int currentPullIndex;

            var needed = GetPulledList();
            currentPullIndex = needed.index;
            currentList = needed.pull;

            foreach (var e in Owner.GetSyncEntitiesHolderComponent().SyncEnities)
            {
                AddAllComponents(e.Value, currentList);
                currentIndex = currentList.Count;

                if (currentIndex >= 8)
                {
                    var command = new SyncServerComponentsCommand { Components = currentList };
                    dataSenderSystem.SendCommandToAll(command, Owner.GUID, DeliveryMethod.Unreliable);
                    ClearPull(currentPullIndex);

                    needed = GetPulledList();
                    currentPullIndex = needed.index;
                    currentList = needed.pull;
                }
            }

            if (currentList.Count > 0)
            {
                var command = new SyncServerComponentsCommand { Components = currentList };
                dataSenderSystem.SendCommandToAll(command, Owner.GUID, DeliveryMethod.Unreliable);
            }

            ClearPull(currentPullIndex);

            //pullResolvers[currentIndex].Clear();
            //lockedLists[currentIndex] = false;
        }

        private void ClearPull(int index)
        {
            lock (lockedLists)
            {
                lockedLists[index] = false;
                pullResolvers[index].Clear();
            }
        }

        private (int index, List<ResolverDataContainer> pull) GetPulledList()
        {
            int currentIndex = -1;

            lock (lockedLists)
            {
                for (int i = 0; i < lockedLists.Length; i++)
                {
                    if (!lockedLists[i])
                    {
                        lockedLists[i] = true;
                        currentIndex = i;
                        break;
                    }
                }
            }

            return (currentIndex, pullResolvers[currentIndex]);
        }

        private void SyncComponents(IEntity entity)
        {
            if (entity == null || !entity.IsAlive)
                return;

            var syncComps = entity.GetNetworkComponentsHolder().GetNetWorkComponents();


            foreach (var c in syncComps)
            {
                if (c.IsDirty)
                {
                    dataSenderSystem.SyncSendComponentToAll(c, entity.GUID, DeliveryMethod.Unreliable);
                    c.IsDirty = false;
                }
            }
        }

        private void AddAllComponents(IEntity entity, List<ResolverDataContainer> resolverDataContainers)
        {
            if (entity == null || !entity.IsAlive || resolverDataContainers == null)
                return;

            var syncComps = entity.GetNetworkComponentsHolder().GetNetWorkComponents();

            lock (resolverDataContainers)
            {
                foreach (var c in syncComps)
                {
                    if (c.IsDirty)
                    {
                        if (c is IUnreliable)
                            resolverDataContainers.Add(EntityManager.ResolversMap.GetComponentContainer(c));
                        else
                            dataSenderSystem.SyncSendComponentToAll(c, entity.GUID, DeliveryMethod.ReliableUnordered);

                        c.IsDirty = false;
                    }
                }
            }

            return;
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
