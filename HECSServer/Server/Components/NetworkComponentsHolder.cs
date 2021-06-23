using HECSFramework.Core;
using HECSFramework.Network;
using System.Collections.Concurrent;

namespace Components
{
    public class NetworkComponentsHolder : BaseComponent, INetworkComponentsHolder
    {
        private ConcurrentBag<INetworkComponent> networkComponents = new ConcurrentBag<INetworkComponent>();
        public bool IsDirty { get; set; } = true;

        public ConcurrentBag<INetworkComponent> GetNetWorkComponents()
        {
            if (!IsDirty)
                return networkComponents;

            networkComponents.Clear();

            lock (Owner.GetAllComponents)
            {
                foreach (var c in Owner.GetAllComponents)
                {
                    if (c is INetworkComponent networkComponent)
                    {
                        networkComponents.Add(networkComponent);
                    }
                }
            }

            return networkComponents;
        }
    }

    public interface INetworkComponentsHolder : IComponent
    {
        bool IsDirty { get; set; }
        public ConcurrentBag<INetworkComponent> GetNetWorkComponents();
    }
}
