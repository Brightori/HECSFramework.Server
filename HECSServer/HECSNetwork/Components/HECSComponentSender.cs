using HECSFrameWork;
using HECSFrameWork.Components;
using HECSServer.ServerShared;
using LiteNetLib;
using System;

namespace HECSServer.HECSNetwork
{
    public class HECSComponentSender : IHECSComponentSender
    {
        public void SyncSendComponent(NetPeer netPeer, Guid sender, INetworkComponent networkComponent, DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable)
        {
            var container = ResolverProvider.ResolversMap.GetComponentContainer(networkComponent);
            var data = MessagePack.MessagePackSerializer.Serialize(container);

            var message = new HECSNetMessage
            {
                ClientGuid = sender,
                ComponentID = networkComponent.GetTypeHashCode,
                Data = data,
                Entity = networkComponent.Owner.EntityGuid,
                Type = HECSNetMessage.TypeOfMessage.Component
            };

            var packet = MessagePack.MessagePackSerializer.Serialize(message);
            netPeer.Send(packet, deliveryMethod);
            EntityManager.Command(new StatisticsCommand{Value = networkComponent.ToString(), StatisticsType = StatisticsType.ComponentSent});
        }

        public void SyncSendComponentToAll(INetworkComponent networkComponent, Guid sender, DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable)
        {
            var container = ResolverProvider.ResolversMap.GetComponentContainer(networkComponent);
            var data = MessagePack.MessagePackSerializer.Serialize(container);

            var message = new HECSNetMessage
            {
                ClientGuid = sender,
                ComponentID = networkComponent.GetTypeHashCode,
                Data = data,
                Entity = networkComponent.Owner.EntityGuid,
                Type = HECSNetMessage.TypeOfMessage.Component
            };

            var packet = MessagePack.MessagePackSerializer.Serialize(message);

            EntityManager.TryGetEntityByComponents(out var server, ComponentID.ServerLogicTagComponentID);
            
            foreach (var netPeer in server.GetConnectionsHolderComponent().ClientConnectionsGUID)
            {
                netPeer.Value.Send(packet, deliveryMethod);
            }
            
            EntityManager.Command(new StatisticsCommand { Value = networkComponent.ToString(), StatisticsType = StatisticsType.ComponentSent });
        }
    }
}