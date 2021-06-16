using HECSFrameWork;
using HECSFrameWork.Components;
using MessagePack;

namespace HECSServer.HECSNetwork
{
    internal class HECSClientComponentProcessor : IComponentProcessor
    {
        private ResolversMap resolversMap = new ResolversMap();

        public void Process(HECSNetMessage message)
        {
            if (EntityManager.TryGetEntityByID(message.Entity, out var entity))
            {
                var container = MessagePackSerializer.Deserialize<ResolverDataContainer>(message.Data);
                resolversMap.ProcessResolverContainer(ref container, ref entity);
            }
        }
    }
}