using HECSFramework.Core;
using HECSFramework.Server;

namespace Components
{
    public class ConfigComponent : BaseComponent, IWorldSingleComponent
    {
        public Config Data { get; internal set; }
    }
}
