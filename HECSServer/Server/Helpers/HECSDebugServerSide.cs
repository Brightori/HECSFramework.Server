using Commands;
using Components;
using System;
using System.Diagnostics;
using Systems;

namespace HECSFramework.Server
{
    public class HECSDebugServerSide : IDebugDispatcher
    {
        private readonly DataSenderSystem dataSenderSystem;
        private readonly bool sendToClient;
        private readonly TimeComponent time;

        public HECSDebugServerSide(DataSenderSystem dataSenderSystem, TimeComponent time, bool sendToClient = false)
        {
            this.dataSenderSystem = dataSenderSystem;
            this.sendToClient = sendToClient;
            this.time = time;
        }

        public void LogDebug(string info, object context)
        {
            if (!Config.Instance.DebugLogLevelEnabled) return;

            var contextPart = context != null ? $"[{context.GetType().Name}]" : string.Empty;
            Console.WriteLine($"[Debug][{DateTime.Now:hh:mm:ss:fff}][{time.TickCount}]{contextPart}: {info}");
        }
        
        public void Log(string info)
        {
            Console.WriteLine($"[INFO][{DateTime.Now:hh:mm:ss:fff}][{time.TickCount}]: {info}");
        }

        public void LogError(string info)
        {
            var text = $"[ERROR][{DateTime.Now:hh:mm:ss:fff}][{time.TickCount}]: {info}";
            Console.WriteLine(text);

            if (sendToClient)
                dataSenderSystem.SendCommandToAllClients(new TextMessageCommand { TextMessage = text });
        }

        public void LogWarning(string info)
        {
            var text = $"[WARN][{DateTime.Now:hh:mm:ss:fff}][{time.TickCount}]: {info}";
            Console.WriteLine(text);

            if (sendToClient)
                dataSenderSystem.SendCommandToAllClients(new TextMessageCommand { TextMessage = text });
        }
    }
}