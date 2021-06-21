using HECSFramework.Core;

namespace HECSFramework.Network
{
    public interface INetworkComponent : IComponent 
    {
        int Version { get; set; }
        bool IsDirty { get; set; }
    }

    public interface ISyncToSelf { }
}