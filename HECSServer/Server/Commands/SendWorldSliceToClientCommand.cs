using HECSFramework.Core;
using LiteNetLib;

namespace Commands
{
    public struct SendWorldSliceToClientCommand : IGlobalCommand
    {
        public NetPeer Peer;
        public int LocationZone;
    }
}