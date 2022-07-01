using Commands;
using Components;
using HECSFramework.Core;
using System.Collections.Concurrent;

namespace Systems
{
    [Documentation(Doc.Server, "The system is responsible for adding, removing entities to the client entity")]
    public class BindEntitiesToClientSystem : BaseSystem, IUpdatable
    {
        private short generatorID = 1;

        private ConcurrentQueue<(Guid client, IEntity entity)> incomingEntities = new ConcurrentQueue<(Guid client, IEntity entity)>();
        private ConcurrencyList<IEntity> entityForRep;

        //  private Dictionary<int, IEntity> nonReplicatedEntities = new Dictionary<int, IEntity>();

        private DataSenderSystem dataSender;
        public override void InitSystem()
        {
            //entityForRep = Owner.World.Filter(new FilterMask(HMasks.NetworkEntityTagComponent, HMasks.ReplicationDataComponent));
            dataSender = Owner.World.GetSingleComponent<RoomInfoComponent>().ServerWorld.GetSingleSystem<DataSenderSystem>();
        }

        public void AddEntity(Guid clientID, IEntity entity)
        {
            incomingEntities.Enqueue((clientID, entity));
        }

        public void UpdateLocal()
        {
            while (incomingEntities.TryDequeue(out var data))
            {
                if (Owner.World.TryGetEntityByID(data.client, out var client))
                {
                    client.GetHECSComponent<ClientEntitiesHolderComponent>().ClientEntities.Add(data.entity);
                    var clientIDholder = data.entity.GetOrAddComponent<ClientIDHolderComponent>();
                    clientIDholder.ClientID = data.client;
                }

                if (data.entity.TryGetSystem(out EntityReplicationSystem entityReplication))
                {

                    do { entityReplication.ID = generatorID++; } while (replicatedEntities.ContainsKey(entityReplication.ID));

                    //Send a command to all entities in the world to create this new entity

                    var createCMD = new CreateReplicationEntity()
                    {
                        EntityID = entityReplication.ID,
                        ContainerID = 0,
                        Components = entityReplication.GetFullComponentsData()
                    };

                    dataSender.SendCommand(ClientEntities.Values, createCMD);
                    //Send a command to the new entity to create all the entities that are in the world

                    foreach (EntityReplicationSystem e in replicatedEntities.Values)
                    {

                        var c = new CreateReplicationEntity()
                        {
                            EntityID = e.ID,
                            Components = e.GetFullComponentsData()
                        };
                    }
                }
            }
        }
    }
}
