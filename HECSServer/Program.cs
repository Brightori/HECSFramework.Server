using HECSFramework.Core;
using HECSServer.Core;
using HECSServer.CurrentGame;
using System;
using System.Threading;

namespace SocketUdpClient
{
    class Program
    {
        public static uint Tick = 0;
        
        static void Main(string[] args)
        {
            Debug.Log("test");
            Config.Load();
            EntityManager entityManager = new EntityManager();
            EntityManager.ResolversMap.InitCommandResolvers();

            var server = new HECSServer.Core.Server(8080, "FlashServer");
            
            while (true)
            {
                try
                {
                    server.Update();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.ToString());
                }

                Thread.Sleep(Config.Instance.ServerTickMilliseconds);
                Tick++;
            }
        }
    }
}