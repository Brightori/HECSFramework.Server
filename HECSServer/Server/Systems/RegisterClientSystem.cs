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
    internal class RegisterClientSystem : BaseSystem, IAfterEntityInit, IReactGlobalCommand<ClientConnectCommand>, IReactCommand<RemoveClientCommand> 
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

        public void CommandGlobalReact(ClientConnectCommand command)
        {
            if (connectionsHolderComponent.EntityToWorldConnections.ContainsKey(command.Client))
            {
                HECSDebug.Log($"New connection: {command.Client}. Ignored: already having this guid.");
                return;
            }

            HECSDebug.Log($"New connection: {command.Client}.");
            ServerData serverData = new ServerData{ServerTickIntervalMilliseconds = Config.Instance.ServerTickMilliseconds, ConfigData = JsonConvert.SerializeObject(Config.Instance, Formatting.Indented)};
            dataSenderSystem.SendCommand(connectionsHolderComponent.ClientConnectionsGUID[command.Client], new ClientConnectSuccessCommand { ServerData = serverData });
            Owner.Command(new NewConnectionCommand { Client = command.Client });
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

            foreach (var world in EntityManager.Worlds)
            {
                var filter = EntityManager.Filter(new FilterMask(HMasks.ClientIDHolderComponent), new FilterMask(HMasks.ServerEntityTagComponent), world.Index);
                foreach (var c in filter)
                    if (c.GetClientIDHolderComponent().ClientID == command.ClientGuidToRemove)
                        entitiesToRemove.Enqueue(c);
            }
            
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
            client.AddHecsComponent(new ClientIDHolderComponent {ClientID = client.GUID});

            client.Init(Owner.WorldId);
            connectionsHolderComponent.EntityToWorldConnections.TryAdd(clientId, Owner.WorldId);
            var guidClientPeer = connectionsHolderComponent.ClientConnectionsGUID[clientId];
            connectionsHolderComponent.WorldToPeerClients[Owner.WorldId].TryAdd(guidClientPeer.GetHashCode(), guidClientPeer);
            return client;
        }
    }
}