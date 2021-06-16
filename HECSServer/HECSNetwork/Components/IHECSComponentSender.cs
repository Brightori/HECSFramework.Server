using LiteNetLib;
using System;
using System.Collections.Generic;

namespace HECSServer.HECSNetwork
{
    public interface IHECSComponentSender
    {
        void SyncSendComponentToAll(INetworkComponent networkComponent, Guid sender, DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable);
        void SyncSendComponent(NetPeer netPeer, Guid sender, INetworkComponent networkComponent, DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable);
    }
}