using HECSFramework.Core;
using System;
using System.Threading;
using Systems;

namespace HECSFramework.Server
{
    class StartServer
    {
        public static uint Tick = 0;
        
        internal static void Start(string[] args)
        {
            Config.Load();
            EntityManager entityManager = new EntityManager();

            var server = new Server(8080, Config.Instance.ServerName);
            Debug.Log("Server start");
            
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