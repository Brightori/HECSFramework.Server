using Commands;
using Components;
using HECSFramework.Core;
using HECSFramework.Server;
using LiteNetLib;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;

namespace Systems
{
    internal class RegisterClientSystem : BaseSystem, IAfterEntityInit, 
        IReactGlobalCommand<RegisterClientOnConnectCommand>, IReactCommand<RemoveClientCommand>
    {
        private ConnectionsHolderComponent connectionsHolderComponent;
        private DataSenderSystem dataSenderSystem;
        private ConcurrentQueue<IEntity> entitiesToRemove = new ConcurrentQueue<IEntity>();

        public override void InitSystem()
        {
            connectionsHolderComponent = EntityManager.GetSingleComponent<ConnectionsHolderComponent>();
            CheckWorldConnections(Owner.WorldId);
        }

        public void AfterEntityInit()
        {
            dataSenderSystem = EntityManager.GetSingleSystem<DataSenderSystem>();
        }

        private void CheckWorldConnections(int world)
        {
            if (!connectionsHolderComponent.WorldToPeerClients.ContainsKey(world))
                connectionsHolderComponent.WorldToPeerClients.TryAdd(world, new ConcurrentDictionary<int, NetPeer>());
        }

        public void CommandReact(RemoveClientCommand command)
        {
            if (!EntityManager.TryGetEntityByID(command.ClientGuidToRemove, out var entityClient))
                return;

            //foreach (var world in EntityManager.Worlds)
            //{
            //    var filter = EntityManager.Filter(new FilterMask(HMasks.ClientIDHolderComponent), new FilterMask(HMasks.ServerEntityTagComponent), world.Index);
            //    foreach (var c in filter)
            //        if (c.GetClientIDHolderComponent().ClientID == command.ClientGuidToRemove)
            //            entitiesToRemove.Enqueue(c);
            //}

            while (entitiesToRemove.TryDequeue(out var entity))
            {
                entity.HecsDestroy();
                connectionsHolderComponent.EntityToWorldConnections.TryRemove(entity.GUID, out var remove);
            }

            connectionsHolderComponent.ClientConnectionsGUID.TryRemove(command.ClientGuidToRemove, out var peer);
            connectionsHolderComponent.ClientConnectionsTimes.TryRemove(command.ClientGuidToRemove, out var times);

            entityClient.HecsDestroy();
            HECSDebug.Log($"Entity removed from client: {entityClient.GUID}");
        }

        private IEntity Register(Guid clientId)
        {
            HECSDebug.LogDebug($"Registered client: {clientId}.", this);
            var client = new Entity($"Client {clientId}");
            client.SetGuid(clientId);
            client.AddHecsComponent(new ClientTagComponent());
            client.AddHecsComponent(new WorldSliceIndexComponent());
            client.AddHecsComponent(new ClientIDHolderComponent { ClientID = client.GUID });

            client.Init(Owner.WorldId);
            connectionsHolderComponent.EntityToWorldConnections.TryAdd(clientId, Owner.WorldId);
            var guidClientPeer = connectionsHolderComponent.ClientConnectionsGUID[clientId];
            connectionsHolderComponent.WorldToPeerClients[Owner.WorldId].TryAdd(guidClientPeer.GetHashCode(), guidClientPeer);
            return client;
        }

        public void CommandGlobalReact(RegisterClientOnConnectCommand command)
        {
            var client = new Entity("Client", command.RoomWorld);
            new DefaultClientContainer().Init(client);
            client.Init();

            //connectionsHolderComponent.

            var config = Owner.World.GetSingleComponent<ConfigComponent>();
            ServerData serverData = new ServerData
            {
                DisconnectTimeoutMs = config.Data.DisconnectTimeOut,
                ServerTickIntervalMilliseconds = Config.Instance.ServerTickMilliseconds,
                ConfigData = JsonConvert.SerializeObject(config.Data, Formatting.Indented)
            };

            dataSenderSystem.SendCommand(command.Connect, 
                new ClientConnectSuccessCommand { ServerData = serverData }, DeliveryMethod.ReliableOrdered);

            EntityManager.Worlds.Data[command.RoomWorld].Command(new NewClientOnServerCommand { Client = client.GUID });
        }
    }
}