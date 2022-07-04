using HECSFramework.Core;
using HECSFramework.Network;
using System;

namespace Components
{

    [Serializable, Documentation(Doc.Server, Doc.HECS, "The component contains a list of all replicated entities in the room")]
    public class ReplicatedEntitiesComponent : BaseComponent, INotReplicable
    {
        private short generatorID = 1;
        private Dictionary<short, IEntity> replicatedEntities = new Dictionary<short, IEntity>();

        public bool RemoveEnity(short entityID)
        {
           return replicatedEntities.Remove(entityID);
        }

    

        internal void Register(ReplicationTagEntityComponent replication)
        {
            short id = 0;
            do { id = generatorID++; } while (replicatedEntities.ContainsKey(id));
            replication.Init(id);
        }

        public IEnumerator<IEntity> GetEnumerator() => replicatedEntities.Values.GetEnumerator();
    }
}