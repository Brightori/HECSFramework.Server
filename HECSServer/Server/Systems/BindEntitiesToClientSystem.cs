using Commands;
using Components;
using HECSFramework.Core;
using LiteNetLib;
using System;
using System.Collections.Concurrent;

namespace Systems
{
    [Documentation(Doc.Server, "The system is responsible for adding entities to the room. Associates an entity with an owner(Client). Registers the entity in the replication system")]
    public class BindEntitiesToClientSystem : BaseSystem, IUpdatable, IReactGlobalCommand<NewClientOnServerCommand>, IGlobalStart
    {
        private ReplicatedEntitiesComponent replicatedEntities;
        private DataSenderSystem dataSender;

        private ConcurrentQueue<(Guid clientID, IEntity entity)> incomingEntities = new ConcurrentQueue<(Guid client, IEntity entity)>();

        private ConcurrencyList<IEntity> clients;

        public void GlobalStart()
        {
            clients = Owner.World.Filter(new FilterMask(HMasks.ClientTagComponent));
            dataSender = Owner.World.GetSingleComponent<RoomInfoComponent>().ServerWorld.GetSingleSystem<DataSenderSystem>();
            replicatedEntities = Owner.World.GetSingleComponent<ReplicatedEntitiesComponent>();
        }

        public override void InitSystem()
        {
           
        }

        public void AddEntity(Guid clientID, IEntity entity)
        {
            incomingEntities.Enqueue((clientID, entity));
        }

        public void UpdateLocal()
        {

            while (incomingEntities.TryDequeue(out var data))
            {
                //Bind a entity to client
                if (Owner.World.TryGetEntityByID(data.clientID, out var client))
                {
                    client.GetHECSComponent<ClientEntitiesHolderComponent>().ClientEntities.Add(data.entity);
                    var clientIDholder = data.entity.GetOrAddComponent<ClientIDHolderComponent>();
                    clientIDholder.ClientID = data.clientID;
                }
                else
                {
                    HECSDebug.LogError($"Could not find entity owner:{data.clientID}");
                }

                ReplicationTagEntityComponent replication = data.entity.GetHECSComponent<ReplicationTagEntityComponent>();
                //If the entity is replicated
                if (replication != null)
                {
                   
                    replicatedEntities.Register(replication);

                    //Send a command to all clients in the world to create this new entity
                    var createCMD = new CreateReplicationEntity()
                    {
                        EntityID = replication.ID,
                        ContainerID = data.entity.GetActorContainerID().ContainerIndex,
                        Components = replication.GetFullComponentsData()
                    };

                    foreach (var c in clients)
                    {
                        if(c == null)
                        {
                            HECSDebug.Log($"Empty slot in client list");
                            continue;
                        }

                        //todo закэшировать и обращаться к полю
                        var server = Owner.World.GetSingleComponent<RoomInfoComponent>().ServerWorld;
                        var peer = server.GetSingleComponent<ConnectionsHolderComponent>().ClientConnectionsGUID[c.GUID];

                        if(peer == null)
                        {
                            HECSDebug.Log($"The client;{c.GUID} does not have a peer");
                            continue;
                        }
                        dataSender.SendCommand(peer, createCMD, DeliveryMethod.ReliableOrdered);
                    }
                }
                else HECSDebug.LogWarning($"Replication data not found");
            }
        }

        //Connecting a new player
        public void CommandGlobalReact(NewClientOnServerCommand command)
        {
            //Send a command to the new client to create all the entities that are in the world
            foreach (ReplicationTagEntityComponent entity in replicatedEntities)
            {

                var cmd = new CreateReplicationEntity()
                {
                    EntityID = entity.ID,
                    ContainerID = entity.Owner.GetActorContainerID().ContainerIndex,
                    Components = entity.GetFullComponentsData()
                };

                if(EntityManager.TryGetEntityByID(command.ClientGUID, out IEntity newClient))
                {
                    dataSender.SendCommand(newClient.GetHECSComponent<ClientConnectionInfoComponent>().ClientNetPeer, cmd, DeliveryMethod.ReliableOrdered);
                }
                else { HECSDebug.LogError($"Could not find connection information from the connected client"); }
            }
        }
    }
}