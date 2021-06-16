using Components;
using HECSFrameWork.Components;
using MessagePack;

namespace HECSServer.HECSNetwork
{
    [Union(1, typeof(TransformComponent))]
    [Union(0, typeof(ActorContainerID))]
    public interface INetworkComponent : IComponent, ISingleComponent
    {
        int Version { get; set; }
        bool IsDirty { get; set; }
    }

    public interface IAfterSyncComponent : INetworkComponent
    {
        void AfterSync();
    }

    public interface ISyncToSelf { }
}