using HECSFramework.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Components
{
    public class SyncEntitiesHolderComponent : BaseComponent 
    {
        private ConcurrentDictionary<Guid, IEntity> syncEntities { get; } = new ConcurrentDictionary<Guid, IEntity>();
        private int currentIndex;

        public ConcurrencyList<Guid> DefaultEntitiesOnLocation = new ConcurrencyList<Guid>();
        public IReadOnlyDictionary<Guid, IEntity> SyncEntities => syncEntities;
        
        public int CurrentIndex
        {
            get => currentIndex;
            private set
            {
                currentIndex = value;
                if (currentIndex > 30000000)
                    currentIndex = 0;
            }
        }

        public void AddEntity(IEntity entity)
        {
            if (!syncEntities.TryAdd(entity.GUID, entity)) return;
            
            entity.GetOrAddComponent<NetworkComponentsHolder>();
            CurrentIndex++;
            UpdateEntitiesIndexes();
        }

        public void RemoveEntity(IEntity entity)
        {
            if (!syncEntities.TryRemove(entity.GUID, out _)) return;

            CurrentIndex++;
            UpdateEntitiesIndexes();
        }
        
        private void UpdateEntitiesIndexes()
        {
            foreach (var kvp in syncEntities)
            {
                if (kvp.Value.TryGetHecsComponent(HMasks.WorldSliceIndexComponent, out WorldSliceIndexComponent sliceIndex))
                    sliceIndex.Index = CurrentIndex;
            }
        }
    }
}