using MessagePack;

namespace HECSServer.HECSNetwork
{
    [MessagePackObject]
    public class NetWorkComponentContainer
    {
        [Key(0)]
        public string Type;
        
        [Key(1)]
        public SyncData SyncData;

        public NetWorkComponentContainer() { }

        public NetWorkComponentContainer(INetworkComponent networkComponent)
        {
        }
    }
}
