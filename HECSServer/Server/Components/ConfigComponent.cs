using HECSFramework.Core;
using HECSFramework.Server;

namespace Components
{
    public class ConfigComponent : BaseComponent
    {
        public Config Data { get; internal set; }
    }
}
