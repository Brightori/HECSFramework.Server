using Commands;
using Components;
using HECSFramework.Core;
using HECSFramework.Network;
using LiteNetLib;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Systems
{
    public class SyncWorldComponentsSystem : BaseSystem, IReactComponent, IReactEntity, IReactGlobalCommand<SyncComponentsCommand>
    {
        private readonly ConcurrencyList<INetworkComponent> networkComponents = new ConcurrencyList<INetworkComponent>();
        private readonly ConcurrentDictionary<Guid, ConcurrencyList<INetworkComponent>> syncSelfOnly = new ConcurrentDictionary<Guid, ConcurrencyList<INetworkComponent>>();
        private readonly ConcurrentDictionary<Guid, ConcurrencyList<INetworkComponent>> ignoreSelfSync = new ConcurrentDictionary<Guid, ConcurrencyList<INetworkComponent>>();

        private DataSenderSystem dataSenderSystem;

        public Guid ListenerGuid => SystemGuid;

        public override void InitSystem()
        {
            dataSenderSystem = EntityManager.GetSingleSystem<DataSenderSystem>();
            var networkEntities = EntityManager.Filter(HMasks.NetworkEntityTagComponent, Owner.WorldId);

            foreach (var networkEntity in networkEntities)
            foreach (var nc in networkEntity.GetComponentsByType<INetworkComponent>())
            {
                if (nc is INotReplicable)
                    continue;

                ComponentReact(nc, true);
            }
        }

        public void ComponentReact<T>(T component, bool isAdded) where T : IComponent
        {
            bool clientHolderIsAdded = isAdded && component.ComponentsMask.Index == HMasks.ClientIDHolderComponent.Index;
            if (clientHolderIsAdded)
            {
                foreach (var ownerComponent in component.Owner.GetAllComponents)
                    if (ownerComponent is INetworkComponent ownerNetworkComponent)
                        OnComponentAdded(ownerNetworkComponent);
            }
            else if (component is INetworkComponent networkComponent)
            {
                if (isAdded)
                    OnComponentAdded(networkComponent);
                else
                    OnComponentRemoved(networkComponent);
            }
        }

        private void OnComponentAdded(INetworkComponent networkComponent)
        {
            switch (networkComponent)
            {
                case IIgnoreSelfSync:
                    if (!networkComponent.Owner.TryGetHecsComponent(HMasks.ClientIDHolderComponent, out ClientIDHolderComponent idHolderComponent))
                    {
                        if (!networkComponents.Contains(networkComponent)) networkComponents.Add(networkComponent);
                        break;
                    }

                    var currentClientID = idHolderComponent.ClientID;
                    ignoreSelfSync.TryAdd(currentClientID, new ConcurrencyList<INetworkComponent>());
                    if (ignoreSelfSync[currentClientID].Contains(networkComponent)) return;

                    ignoreSelfSync[currentClientID].Add(networkComponent);
                    break;
                case ISyncOnlySelf:
                    if (!networkComponent.Owner.TryGetHecsComponent(HMasks.ClientIDHolderComponent, out ClientIDHolderComponent idHolder)) return;

                    syncSelfOnly.TryAdd(idHolder.ClientID, new ConcurrencyList<INetworkComponent>());
                    if (syncSelfOnly[idHolder.ClientID].Contains(networkComponent)) return;

                    syncSelfOnly[idHolder.ClientID].Add(networkComponent);
                    break;
                default:
                    if (!networkComponents.Contains(networkComponent)) networkComponents.Add(networkComponent);
                    break;
            }
        }

        private void OnComponentRemoved(INetworkComponent networkComponent)
        {
            switch (networkComponent)
            {
                case IIgnoreSelfSync:
                    foreach (var (_, components) in ignoreSelfSync)
                        if (components.Contains(networkComponent))
                            components.Remove(networkComponent);
                    break;
                case ISyncOnlySelf:
                    foreach (var (_, components) in syncSelfOnly)
                        if (components.Contains(networkComponent))
                            components.Remove(networkComponent);
                    break;
            }
            networkComponents.Remove(networkComponent);
        }

        public void EntityReact(IEntity entity, bool isAdded)
        {
            if (entity.ContainsMask(ref HMasks.NetworkEntityTagComponent))
            {
                foreach (var nc in entity.GetComponentsByType<INetworkComponent>())
                    ComponentReact(nc, isAdded);
            }
        }

        public void SyncComponents()
        {
            SyncNetworkComponents();
            SyncToSelf();
            IgnoreSelfSync();
        }

        private void SyncNetworkComponents()
        {
            var currentCount = networkComponents.Count;
            var notReady = EntityManager.Filter(HMasks.ClientTagComponent)
                .ToArray()
                .Where(a => !a.GetClientTagComponent().IsReadyToSync)
                .Select(a => a.GUID)
                .ToArray();

            for (int i = 0; i < currentCount; i++)
            {
                if (!networkComponents.Data[i].IsDirty || !networkComponents.Data[i].IsAlive) continue;

                ++networkComponents.Data[i].Version;

                var deliveryMethod = networkComponents.Data[i] is IUnreliable ? DeliveryMethod.Unreliable : DeliveryMethod.ReliableUnordered;
                dataSenderSystem.SendComponentToFilteredClients(networkComponents.Data[i], deliveryMethod, notReady);

                networkComponents.Data[i].IsDirty = false;
            }

        }

        private void IgnoreSelfSync()
        {
            foreach (var sync in ignoreSelfSync)
            {
                if (!EntityManager.TryGetEntityByID(sync.Key, out var client))
                    continue;
                
                if (!client.GetClientTagComponent().IsReadyToSync)
                    continue;

                foreach (var nc in sync.Value)
                {
                    if (!nc.IsDirty) continue;

                    ++nc.Version;

                    var deliveryMethod = nc is IUnreliable ? DeliveryMethod.Unreliable : DeliveryMethod.ReliableUnordered;
                    dataSenderSystem.SendComponentToFilteredClients(nc, deliveryMethod, sync.Key);

                    nc.IsDirty = false;
                }
            }
        }

        private void SyncToSelf()
        {
            foreach (var sync in syncSelfOnly)
            {
                if (!EntityManager.TryGetEntityByID(sync.Key, out var client))
                    continue;
                
                if (!client.GetClientTagComponent().IsReadyToSync)
                    continue;
                
                foreach (var nc in sync.Value)
                {
                    if (!nc.IsDirty) continue;

                    ++nc.Version;

                    var deliveryMethod = nc is IUnreliable ? DeliveryMethod.Unreliable : DeliveryMethod.ReliableUnordered;
                    dataSenderSystem.SendComponent(sync.Key, nc, deliveryMethod);

                    nc.IsDirty = false;
                }
            }
        }

        public void CommandGlobalReact(SyncComponentsCommand command)
        {
            SyncComponents();
        }
    }
}