using Components;
using HECSFrameWork;
using HECSFrameWork.Components;
using HECSServer.Core;
using HECSServer.ServerShared;
using LiteNetLib;
using MessagePack;
using System;

namespace HECSServer.HECSNetwork
{
    public class HECSMessageSender : IHECSMessageSender
    {
        public static CommandMap CommandMap = new CommandMap();

        public void SendCommandToAll<T>(T networkCommand, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableUnordered) where T : INetworkCommand
        {
            if (!EntityManager.TryGetEntityByComponents(out var entity, ComponentID.ConnectionsHolderComponentID))
            {
                Debug.LogError("ConnectionsHolderComponentID not detected");
                return;
            }

            var commandContainer = CommandMap.GetHECSCommandMessage(networkCommand, Guid.Empty);
            EntityManager.Command(new StatisticsCommand { Value = networkCommand.ToString(), StatisticsType = StatisticsType.CommandSent });

            foreach (var kvp in entity.GetConnectionsHolderComponent().ClientConnectionsGUID)
                kvp.Value.Send(Data(commandContainer), deliveryMethod);
        }

        public void SendCommand<T>(Guid client, T networkCommand) where T : INetworkCommand
        {
            if (!EntityManager.TryGetEntityByComponents(out var entity, ComponentID.ConnectionsHolderComponentID))
            {
                Debug.LogError("ConnectionsHolderComponentID not detected");
                return;
            }
            
            var peer = entity.GetConnectionsHolderComponent().ClientConnectionsGUID[client];
            SendCommand(peer, networkCommand);
        }

        public void SendCommand<T>(NetPeer peer, T networkCommand, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableUnordered) where T : INetworkCommand
        {
            var commandContainer = CommandMap.GetHECSCommandMessage(networkCommand, Guid.Empty);
            peer.Send(Data(commandContainer), deliveryMethod);
            EntityManager.Command(new StatisticsCommand{Value = networkCommand.ToString(), StatisticsType = StatisticsType.CommandSent});
        }

        public static byte[] Data(HECSNetMessage message)
        {
            return MessagePackSerializer.Serialize(message);
        }
        
        public static void SendMessageToAllClients(string text)
        {
            var data = MessagePackSerializer.Serialize(text);
            var message = new HECSNetMessage
            {
                Type = HECSNetMessage.TypeOfMessage.String,
                Data = data,
            };

            var packet = MessagePackSerializer.Serialize(message);
            EntityManager.TryGetComponent(a => a.IsHaveComponents(ComponentID.ConnectionsHolderComponentID), out IConnectionsHolderComponent holder, 0);
            foreach (var kvp in holder.ClientConnectionsID)
            {
                kvp.Value.Send(packet, DeliveryMethod.ReliableOrdered);
            }
        }
    }

    public interface IHECSMessageSender
    {
        void SendCommandToAll<T>(T networkCommand, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableUnordered) where T : INetworkCommand;
        void SendCommand<T>(Guid client, T networkCommand) where T : INetworkCommand;
        void SendCommand<T>(NetPeer peer, T networkCommand, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableUnordered) where T : INetworkCommand;

    }
}