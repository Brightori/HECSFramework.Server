using HECSFramework.Core;
using LiteNetLib;
using System;

namespace HECSServer.HECSNetwork
{
    public interface IHECSSender
    {
        void SendToAll(ResolverDataContainer container, Guid sender, DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable);
        void SendToPeer(NetPeer netPeer, Guid sender, ResolverDataContainer container, DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable);
    }
}