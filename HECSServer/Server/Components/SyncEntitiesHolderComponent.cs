using HECSFramework.Core;
using System;
using System.Collections.Concurrent;

namespace Components
{
    public class SyncEntitiesHolderComponent : BaseComponent, ISyncEntitiesHolderComponent
    {
        /// <summary>
        /// не добавлять сюда данные напрямую, только через методы, отсюда только читаем актуальные данные
        /// </summary>
        public ConcurrentDictionary<Guid, IEntity> SyncEnities { get; } = new ConcurrentDictionary<Guid, IEntity>();
        public ConcurrentDictionary<Guid, int> EntityToSlice { get; } = new ConcurrentDictionary<Guid, int>();
        public ConcurrentDictionary<int, EntitiesOnIndex> DeltaSlice { get; } = new ConcurrentDictionary<int, EntitiesOnIndex>();
        private int currentIndex;

        public int CurrentIndex
        {
            get => currentIndex;
            private set
            {
                currentIndex = value;
                if (currentIndex > 30000000)
                    currentIndex = 0;
                UpdateEntitiesIndexes();
            }
        }

        public void AddEntity(IEntity entity)
        {
            SyncEnities.TryAdd(entity.GUID, entity);

            entity.GetOrAddComponent<NetworkComponentsHolder>();


            var microSlice = new EntitiesOnIndex { AddEntity = entity.GUID };

            if (entity.TryGetHecsComponent(HMasks.ClientIDHolderComponent, out IClientIDHolderComponent clientIDHolderComponent))
            {
                if (clientIDHolderComponent.Client.TryGetHecsComponent(HMasks.LocationComponent, out LocationComponent locationComponent))
                    microSlice.Location = locationComponent.LocationZone;
            }

            DeltaSlice.TryAdd(CurrentIndex, microSlice);
            CurrentIndex++;
        }

        public void RemoveEntity(IEntity entity)
        {
            if (SyncEnities.TryRemove(entity.GUID, out var entityResult))
            {
                DeltaSlice.TryAdd(CurrentIndex, new EntitiesOnIndex { RemoveEntity = entity.GUID });
                CurrentIndex++;
            }
        }

        public void RemoveEntity(Guid entity)
        {
            if (SyncEnities.TryRemove(entity, out var entityResult))
            {
                DeltaSlice.TryAdd(CurrentIndex, new EntitiesOnIndex { RemoveEntity = entity });
                CurrentIndex++;
            }
        }

        public int UpdateIndex()
        {
            ++CurrentIndex;
            return CurrentIndex;
        }

        private void UpdateEntitiesIndexes()
        {
            foreach (var kvp in SyncEnities)
            {
                if (kvp.Value.TryGetHecsComponent(HMasks.WorldSliceIndexComponent, out IWorldSliceIndexComponent sliceIndex))
                    sliceIndex.Index = CurrentIndex;
            }
        }
    }

    public struct EntitiesOnIndex
    {
        public Guid AddEntity;
        public Guid RemoveEntity;
        public int Location;
    }

    public interface ISyncEntitiesHolderComponent : IComponent
    {
        ConcurrentDictionary<Guid, IEntity> SyncEnities { get; }
        ConcurrentDictionary<Guid, int> EntityToSlice { get; }
        ConcurrentDictionary<int, EntitiesOnIndex> DeltaSlice { get; }
        int UpdateIndex();
        int CurrentIndex { get; }
        void AddEntity(IEntity entity);
        void RemoveEntity(IEntity entity);
        void RemoveEntity(Guid entity);
    }
}