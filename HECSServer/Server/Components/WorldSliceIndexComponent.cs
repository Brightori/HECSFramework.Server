using HECSFramework.Core;
using HECSFramework.Network;
using System;

namespace Components
{
    [Serializable]
    public class WorldSliceIndexComponent : BaseComponent, IWorldSliceIndexComponent
    {
        private int index;

        [Field(0)]
        public int Index
        {
            get => index;
            set
            {
                IsDirty = index != value;
                index = value;
            }
        }

        public int Version { get; set; }
        public bool IsDirty { get; set; }
    }

    public interface IWorldSliceIndexComponent : INetworkComponent
    {
        int Index { get; set; }
    }
}