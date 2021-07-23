using Commands;
using Components;
using HECSFramework.Core;
using HECSFramework.Server;
using LiteNetLib;
using System.Collections.Concurrent;

namespace Systems
{
    internal class RegisterClientSystem : BaseSystem, IAfterEntityInit,
        IReactGlobalCommand<ClientConnectCommand>, IReactCommand<RemoveClientCommand>
    {
        private ConnectionsHolderComponent connectionsHolderComponent;
        private IDataSenderSystem dataSenderSystem;
        private ConcurrentQueue<IEntity> entitiesToRemove = new ConcurrentQueue<IEntity>();

        public override void InitSystem()
        {
            connectionsHolderComponent = Owner.GetHECSComponent<ConnectionsHolderComponent>();
            CheckWorldConnections(Owner.WorldId);
        }

        public void AfterEntityInit()
        {
            Owner.TryGetSystem(out dataSenderSystem);
        }

        public void CommandGlobalReact(ClientConnectCommand command)
        {
            if (connectionsHolderComponent.EntityToWorldConnections.ContainsKey(command.Client))
            {
                Debug.Log($"Прилетело новое подключение: {command.Client}. Такой клиент уже есть, игнорируем.");
                return;
            }

            Debug.Log($"Прилетело новое подключение: {command.Client}.");
            var client = new Entity($"Client {command.Client}");
            client.SetGuid(command.Client);
            client.AddHecsComponent(new ClientTagComponent());
            client.AddHecsComponent(new WorldSliceIndexComponent());

            client.Init(Owner.WorldId);

            connectionsHolderComponent.EntityToWorldConnections.TryAdd(command.Client, Owner.WorldId);

            var guidClientPeer = connectionsHolderComponent.ClientConnectionsGUID[command.Client];

            connectionsHolderComponent.WorldToPeerClients[Owner.WorldId].TryAdd(guidClientPeer.GetHashCode(), guidClientPeer);
            dataSenderSystem.SendCommand(connectionsHolderComponent.ClientConnectionsGUID[command.Client], SystemGuid, new ClientConnectSuccessCommand { ServerTickIntervalMilliseconds = Config.Instance.ServerTickMilliseconds });
            Owner.Command(new NewClientOnServerCommand { Client = command.Client });
        }

        private void CheckWorldConnections(int world)
        {
            if (!connectionsHolderComponent.WorldToPeerClients.ContainsKey(world))
                connectionsHolderComponent.WorldToPeerClients.TryAdd(world, new ConcurrentDictionary<int, NetPeer>());
        }

        public void CommandReact(RemoveClientCommand command)
        {
            var filter = EntityManager.Filter(HMasks.ClientIDHolderComponent);

            foreach (var c in filter)
            {
                if (c.GetClientIDHolderComponent().ClientID == command.ClientGuidToRemove)
                {
                    entitiesToRemove.Enqueue(c);
                }
            }

            while (entitiesToRemove.TryDequeue(out var entity))
            {
                entity.HecsDestroy();
                connectionsHolderComponent.EntityToWorldConnections.TryRemove(entity.GUID, out var remove);
            }

            connectionsHolderComponent.ClientConnectionsGUID.TryRemove(command.ClientGuidToRemove, out var peer);
            connectionsHolderComponent.ClientConnectionsTimes.TryRemove(command.ClientGuidToRemove, out var times);

            if (EntityManager.TryGetEntityByID(command.ClientGuidToRemove, out var entityClient))
            {
                entityClient.HecsDestroy();
                Debug.Log("удалили ентити с клиента " + entityClient.GUID);
            }
        }
    }
}