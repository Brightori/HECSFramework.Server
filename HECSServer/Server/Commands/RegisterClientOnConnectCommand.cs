using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HECSFramework.Core;
using LiteNetLib;

namespace Commands
{
    public struct RegisterClientOnConnectCommand : IGlobalCommand
    {
        public NetPeer Connect;
        public int RoomWorld;
    }
}
