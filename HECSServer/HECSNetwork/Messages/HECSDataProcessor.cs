using HECSFramework.Core;
using HECSServer.Core;

namespace HECSFramework.Network
{
    public class HECSDataProcessor : IMessageProcessor
    {
        void IMessageProcessor.Process(ResolverDataContainer message)
        {
            switch (message.Type)
            {
                case 0:
                    EntityManager.TryGetEntityByID(message.EntityGuid, out var entity);
                    EntityManager.ResolversMap.ProcessResolverContainer(ref message, ref entity);
                    break;
                case 1:
                    break;
                case 2:
                    EntityManager.ResolversMap.ProcessCommand(message);
                    break;

            }
        }
    }
}

namespace HECSServer.Core
{
    public interface IMessageProcessor
    {
        void Process(ResolverDataContainer message);
    }
}