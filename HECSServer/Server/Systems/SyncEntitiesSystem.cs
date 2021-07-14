using Commands;
using Components;
using HECSFramework.Core;
using HECSFramework.Network;
using HECSFramework.Server;
using LiteNetLib;
using System;

namespace Systems
{
    /// <summary>
    /// эта система отвечает за ентити которые синхронизируются и за пометку грязным - нетворк компонент холдера
    /// </summary>
    public class SyncEntitiesSystem : BaseSystem, IUpdatable, IReactEntity, IReactComponent
    {
        private ConnectionsHolderComponent connectionsHolderComponent;
        private SyncEntitiesHolderComponent syncEntitiesHolderComponent;
        private IDataSenderSystem dataSenderSystem;

        private float step => Config.Instance.ServerTickMilliseconds;
        public Guid ListenerGuid => SystemGuid;

        private ConcurrencyList<IEntity> clients;

        public override void InitSystem()
        {
            Owner.TryGetSystem(out dataSenderSystem);
            Owner.TryGetHecsComponent(out connectionsHolderComponent);
            Owner.TryGetHecsComponent(out syncEntitiesHolderComponent);
            clients = EntityManager.Filter(HMasks.ClientTagComponent);
        }

        public void UpdateLocal()
        {
            CheckSync();
        }

        private void CheckSync()
        {
            var currentCount = clients.Count;
            var currentIndex = syncEntitiesHolderComponent.CurrentIndex;


            for (int i = 0; i < currentCount; i++)
            {
                if (clients[i] != null && clients[i].IsAlive && clients[i].GetWorldSliceIndexComponent().Index != syncEntitiesHolderComponent.CurrentIndex)
                {
                    var currentClientEntities = clients[i].GetWorldSliceIndexComponent().EntitiesOnClient;
                    var currentClientEntitiesToRemove = clients[i].GetWorldSliceIndexComponent().EntitiesToRemove;
                    var clientPeer = clients[i].GetClientTagComponent().NetPeer;

                    foreach (var entity in syncEntitiesHolderComponent.SyncEnities)
                    {
                        if (currentClientEntities.Contains(entity.Key))
                            continue;

                        //надо понять нужно это или нет (игнорирование ентити принадлежащиъ этому клиенту
                        //if(entity.Value.TryGetHecsComponent(HMasks.ClientIDHolderComponent, 
                        //    out ClientIDHolderComponent clientIDHolderComponent))
                        //{
                        //    if (clientIDHolderComponent.ClientID == clients[i].GUID)
                        //        continue;
                        //}

                        var resolver = new EntityResolver().GetEntityResolver(entity.Value);


                        dataSenderSystem.SendCommand(clientPeer, Guid.Empty, new SpawnEntityCommand
                        {
                            CharacterGuid = entity.Key,
                            ClientGuid = clients[i].GUID,
                            Entity = new EntityResolver().GetEntityResolver(entity.Value),
                            Index = entity.Value.WorldId,
                            IsNeedRecieveConfirm = false,
                        }, DeliveryMethod.Unreliable);


                        currentClientEntities.Add(entity.Key);
                    }

                    foreach (var c in currentClientEntities)
                    {
                        if (syncEntitiesHolderComponent.SyncEnities.ContainsKey(c))
                            continue;

                        dataSenderSystem.SendCommand(clientPeer, Guid.Empty, new RemoveEntityFromClientCommand { EntityToRemove = c });
                        currentClientEntitiesToRemove.Add(c);
                    }

                    //foreach (var c in currentClientEntitiesToRemove)
                    //{
                    //    currentClientEntities.Remove(c);
                    //}

                    //currentClientEntitiesToRemove.Clear();
                }

                clients[i].GetWorldSliceIndexComponent().Index = currentIndex;
            }

            //return Task.CompletedTask;
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
    }

    public struct SpawnCommandToPeerContainer
    {
        public NetPeer Peer;
        public SpawnEntityCommand SpawnEntityCommand;
    }
}