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
        }

        public void AfterEntityInit()
        {
            dataSenderSystem = EntityManager.GetSingleSystem<DataSenderSystem>();
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

        public void CommandGlobalReact(RegisterClientOnConnectCommand command)
        {
            var client = new Entity("Client", command.RoomWorld);
            new DefaultClientContainer().Init(client);
            client.Init();

            connectionsHolderComponent.RegisterClient(client, command.Connect, client.World);

            var config = Owner.World.GetSingleComponent<ConfigComponent>();
            ServerData serverData = new ServerData
            {
                DisconnectTimeoutMs = config.Data.DisconnectTimeOut,
                ServerTickIntervalMilliseconds = Config.Instance.ServerTickMilliseconds,
                ConfigData = JsonConvert.SerializeObject(config.Data, Formatting.Indented)
            };

            HECSDebug.Log($"Register new client:{client.GUID} on server");
            dataSenderSystem.SendCommand(command.Connect, 
                new ClientConnectSuccessCommand { ServerData = serverData, Guid = client.GUID }, DeliveryMethod.ReliableOrdered);

            EntityManager.Worlds.Data[command.RoomWorld].Command(new NewClientOnServerCommand { ClientGUID = client.GUID });
        }
    }
}