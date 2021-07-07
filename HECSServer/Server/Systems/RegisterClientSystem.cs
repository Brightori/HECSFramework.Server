using Commands;
using Components;
using HECSFramework.Core;
using HECSFramework.Server;
using LiteNetLib;
using System.Collections.Concurrent;

namespace Systems
{
    internal class RegisterClientSystem : BaseSystem,
        IReactGlobalCommand<ClientConnectCommand>, IAfterEntityInit
    {
        private ConnectionsHolderComponent connectionsHolderComponent;
        private IDataSenderSystem dataSenderSystem;

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

            client.Init(Owner.WorldId);

            connectionsHolderComponent.EntityToWorldConnections.TryAdd(command.Client, Owner.WorldId);

            var guidClientPeer = connectionsHolderComponent.ClientConnectionsGUID[command.Client];

            connectionsHolderComponent.WorldToPeerClients[Owner.WorldId].TryAdd(guidClientPeer.GetHashCode(), guidClientPeer);
            dataSenderSystem.SendCommand(connectionsHolderComponent.ClientConnectionsGUID[command.Client], SystemGuid, new ClientConnectSuccessCommand { ServerTickIntervalMilliseconds = Config.Instance.ServerTickMilliseconds });

            SyncEntityLocation(command);
        }

        private void SyncEntityLocation(ClientConnectCommand command)
        {
            var peer = connectionsHolderComponent.ClientConnectionsGUID[command.Client];
            
            //todo сюда определение на каком мире живет клиент
            //var location = Owner.GetLocationComponent().LocationZone;
            EntityManager.Command(new SendWorldSliceToClientCommand { Peer = peer, LocationZone = 0 });
        }

        private void CheckWorldConnections(int world)
        {
            if (!connectionsHolderComponent.WorldToPeerClients.ContainsKey(world))
                connectionsHolderComponent.WorldToPeerClients.TryAdd(world, new ConcurrentDictionary<int, NetPeer>());
        }
    }
}