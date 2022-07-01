using HECSFramework.Core;
using LiteNetLib;

namespace Components
{
    public sealed class ClientConnectionInfoComponent : BaseComponent
    {
        public NetPeer ClientNetPeer;
    }
}
