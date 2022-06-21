using Commands;
using Components;
using HECSFramework.Core;
using HECSFramework.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Systems
{
    [Documentation(Doc.Server, "Отвечает за ентити которые синхронизируются и за пометку грязным нетворк компонент холдера")]
    public class SyncEntitiesSystem : BaseSystem, IUpdatable, IReactEntity, IReactComponent, IReactGlobalCommand<DestroyNetworkEntityWorldCommand>, IReactGlobalCommand<SyncClientCommand>
    {
        private SyncEntitiesHolderComponent syncEntitiesHolderComponent;
        private DataSenderSystem dataSenderSystem;

        public Guid ListenerGuid => SystemGuid;

        private ConcurrencyList<IEntity> clients;
        private HashSet<Guid> alrdyDeleted = new HashSet<Guid>();

        public override void InitSystem()
        {
            dataSenderSystem = EntityManager.GetSingleSystem<DataSenderSystem>();
            Owner.TryGetHecsComponent(out syncEntitiesHolderComponent);
            clients = EntityManager.Filter(HMasks.ClientTagComponent);
        }

        public void UpdateLocal()
        {
            SyncAllClients();
        }

        private void SyncAllClients()
        {
            var currentCount = clients.Count;
            var currentIndex = syncEntitiesHolderComponent.CurrentIndex;

            for (int i = 0; i < currentCount; i++)
            {
                var client = clients[i];
                if (!client.IsAlive()) continue;
                if (client.GetWorldSliceIndexComponent().Index == syncEntitiesHolderComponent.CurrentIndex) continue;
                if (!client.GetClientTagComponent().IsReadyToSync) continue;

                SyncClient(client);
                if (client.TryGetHecsComponent(HMasks.WorldSliceIndexComponent, out WorldSliceIndexComponent worldSliceIndexComponent))
                    worldSliceIndexComponent.Index = currentIndex;
            }

            alrdyDeleted.Clear();
        }

        private void SyncClient(IEntity client)
        {
            // TODO: более централизованно удалять энтити
            var currentClientEntities = client.GetWorldSliceIndexComponent().EntitiesOnClient;
            var currentClientEntitiesToRemove = client.GetWorldSliceIndexComponent().EntitiesToRemove;

            foreach (var entity in syncEntitiesHolderComponent.SyncEntities)
            {
                if (currentClientEntities.Contains(entity.Key)) continue;

                currentClientEntities.Add(entity.Key);

                if (entity.Value.ContainsMask(ref HMasks.PrebakedEntityCanBeRemovedTagComponent)) continue;

                var spawnCommand = new SpawnEntityCommand
                {
                    CharacterGuid = entity.Key,
                    Entity = new EntityResolver().GetEntityResolver(entity.Value),
                    WorldId = entity.Value.WorldId
                };
                dataSenderSystem.SendCommand(client.GUID, spawnCommand);
            }

            while (currentClientEntities.Contains(Guid.Empty)) currentClientEntities.Remove(Guid.Empty);
            foreach (Guid entity in currentClientEntities)
            {
                if (syncEntitiesHolderComponent.SyncEntities.ContainsKey(entity)) continue;

                if (!alrdyDeleted.Contains(entity))
                {
                    dataSenderSystem.SendCommand(client.GUID, new RemoveEntityFromClientCommand { EntityToRemove = entity });
                }

                currentClientEntitiesToRemove.Add(entity);
            }

            foreach (var toRemove in currentClientEntitiesToRemove) currentClientEntities.Remove(toRemove);
            currentClientEntitiesToRemove.Clear();
        }

        public void EntityReact(IEntity entity, bool add)
        {
            if (!add)
                syncEntitiesHolderComponent.RemoveEntity(entity);
        }

        public void ComponentReact(IComponent component, bool isAdded)
        {
            if (component.Owner.IsAlive && component is INetworkComponent && component.Owner.ContainsMask(ref HMasks.NetworkComponentsHolder))
                component.Owner.GetNetworkComponentsHolder().IsDirty = true;
        }

        public void CommandGlobalReact(DestroyNetworkEntityWorldCommand command)
        {
            alrdyDeleted.Add(command.Entity.GUID);
        }

        public void CommandGlobalReact(SyncClientCommand command)
        {
            foreach (var client in clients)
            {
                if (client.GUID != command.Client)
                    continue;

                SyncClient(client);
                var guids = syncEntitiesHolderComponent.SyncEntities.Values.Select(a => a.GUID).ToArray();
                dataSenderSystem.SendCommand(client.GUID, new SyncEntitiesStartedNetworkCommand{Entities = guids});
                return;
            }

            HECSDebug.LogWarning($"Client to sync not found: {command.Client}");
        }
    }
}