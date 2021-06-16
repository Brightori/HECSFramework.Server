using HECSServer.Core;
using HECSServer.HECSNetwork;
using System;
using HECSServer.Core.Systems;

namespace HECSServer.HECSNetwork
{
    public class HECSMessageProcessor : IMessageProcessor
    {
        private IComponentProcessor componentProcessor;
        private ICommandProcessor commandProcessor;

        public HECSMessageProcessor(IComponentProcessor componentProcessor, ICommandProcessor commandProcessor)
        {
            this.componentProcessor = componentProcessor;
            this.commandProcessor = commandProcessor;
        }

        void IMessageProcessor.Process(HECSNetMessage message)
        {
            switch (message.Type)
            {
                case HECSNetMessage.TypeOfMessage.Connect:
                case HECSNetMessage.TypeOfMessage.Command:
                    commandProcessor.Process(message);
                    break;
                case HECSNetMessage.TypeOfMessage.Component:
                    componentProcessor.Process(message);
                    break;
                case HECSNetMessage.TypeOfMessage.String:
                    var text = MessagePack.MessagePackSerializer.Deserialize<string>(message.Data);
                    Debug.Log($"Received message: {text}");
                    break;
            }
        }
    }
}

namespace HECSServer.Core
{
    public interface IMessageProcessor
    {
        void Process(HECSNetMessage message);
    }
}