using HECSServer.HECSNetwork;
using HECSServer.ServerShared;
using System;

namespace HECSServer.Core
{
    internal class HECSCommandClientProcessor : HECSCommandProcessor, ICommandProcessor
    {
        private CommandMap commandMap = new CommandMap();

        public override void Process(HECSNetMessage message)
        {
            if (commandMap.Map.TryGetValue(message.CommandID, out var resolver))
                resolver.ResolveCommand(message.Data);
            else
                throw new Exception("нет резолвера команды с следующим ID " + message.CommandID);
        }
    }
}