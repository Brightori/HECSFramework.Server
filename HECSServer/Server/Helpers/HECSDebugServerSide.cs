using Commands;
using System;
using System.Diagnostics;
using Systems;

namespace HECSFramework.Server
{
    public class HECSDebugServerSide : IDebugDispatcher
    {
        private readonly DataSenderSystem dataSenderSystem;
        private readonly bool sendToClient;

        public HECSDebugServerSide(DataSenderSystem dataSenderSystem, bool sendToClient = false)
        {
            this.dataSenderSystem = dataSenderSystem;
            this.sendToClient = sendToClient;
        }

        public void LogDebug(string info, object context)
        {
            if (!Config.Instance.DebugLogLevelEnabled) return;

            var contextPart = context != null ? $"[{context.GetType().Name}]" : string.Empty;
            Console.WriteLine($"[Debug][{DateTime.Now:hh:mm:ss:fff}][{StartServer.Tick}]{contextPart}: {info}");
        }
        
        public void Log(string info)
        {
            Console.WriteLine($"[INFO][{DateTime.Now:hh:mm:ss:fff}][{StartServer.Tick}]: {info}");
        }

        public void LogError(string info)
        {
            var text = $"[ERROR][{DateTime.Now:hh:mm:ss:fff}][{StartServer.Tick}]: {info}";
            Console.WriteLine(text);

            if (sendToClient)
                dataSenderSystem.SendCommandToAllClients(new TextMessageCommand { TextMessage = text });
        }

        public void LogWarning(string info)
        {
            var text = $"[WARN][{DateTime.Now:hh:mm:ss:fff}][{StartServer.Tick}]: {info}";
            Console.WriteLine(text);

            if (sendToClient)
                dataSenderSystem.SendCommandToAllClients(new TextMessageCommand { TextMessage = text });
        }
    }
}