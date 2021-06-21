using System;
using System.ComponentModel.DataAnnotations;
using HECSServer.CurrentGame;
using HECSServer.HECSNetwork;
using SocketUdpClient;

namespace HECSServer.Core
{
    internal static class Debug
    {
        internal static void LogDebugFormat<T>(string format, T obj)
        {
            if (!Config.Instance.DebugLogLevelEnabled) return;
            
            format = format.Replace("{0}", obj.ToString());
            LogDebug(format);
        }
        
        internal static void LogDebugFormat<T0, T1>(string format, T0 obj0, T1 obj1)
        {
            if (!Config.Instance.DebugLogLevelEnabled) return;
            
            format = format.Replace("{0}", obj0.ToString());
            format = format.Replace("{1}", obj1.ToString());
            LogDebug(format);
        }

        internal static void LogDebugFormat<T0, T1, T2>(string format, T0 obj0, T1 obj1, T2 obj2)
        {
            if (!Config.Instance.DebugLogLevelEnabled) return;
            
            format = format.Replace("{0}", obj0.ToString());
            format = format.Replace("{1}", obj1.ToString());
            format = format.Replace("{2}", obj2.ToString());
            LogDebug(format);
        }

        internal static void LogDebug(string msg)
        {
            if (!Config.Instance.DebugLogLevelEnabled) return;
            
            Console.WriteLine($"[DEBUG][{DateTime.Now:hh:mm:ss}][{Program.Tick}]: {msg}");
        }

        internal static void Log(object msg)
            => Console.WriteLine($"[INFO][{DateTime.Now:hh:mm:ss}][{Program.Tick}]: {msg}");

        internal static void LogWarning(string msg, bool send = true)
        {
            var text = $"[WARN][{DateTime.Now:hh:mm:ss}][{Program.Tick}]: {msg}";
            Console.WriteLine(text);
            if (send)
                HECSMessageSender.SendMessageToAllClients(text);
        }
        
        internal static void LogError(string msg, bool send = true)
        {
            var text = $"[ERROR][{DateTime.Now:hh:mm:ss}][{Program.Tick}]: {msg}";
            Console.WriteLine(text);
            if (send)
                HECSMessageSender.SendMessageToAllClients(text);
        }
    }
}