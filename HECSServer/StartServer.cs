using HECSFramework.Core;
using Systems;

namespace HECSFramework.Server
{
    public static class StartServer
    {
      //  public static uint Tick = 0;
        
        public static void Start(string[] args)
        {
            Config config = Config.Load();
            EntityManager entityManager = new EntityManager(16);

            var server = new Server(8080, config);
            HECSDebug.Log("Server start");

            while (true)
            {
                try
                {
                    server.Update();
                }
                catch (Exception e)
                {
                    HECSDebug.LogError(e.ToString());
                }

                Thread.Sleep(Config.Instance.ServerTickMilliseconds);
             //   Tick++;

            }
        }

    }
}