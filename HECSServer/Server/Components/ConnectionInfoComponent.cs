using HECSFramework.Core;

namespace Components
{
    public sealed class ConnectionInfoComponent : BaseComponent, IWorldSingleComponent
    {
        public int Port = 0;
        public string IP = "127.0.0.1";
        public string Pass = "ClausUmbrella";
    }
}