using Commands;
using HECSFrameWork.Systems;
using HECSServer.CurrentGame;

namespace HECSServer.Core
{
    public class Server
    {
        private readonly CurrentGameController gamesController;
        private readonly GlobalUpdateSystem globalUpdateSystem = new GlobalUpdateSystem();
        public static ulong ServerTime;

        public Server(int port, string key)
        {
            globalUpdateSystem.Init();
            globalUpdateSystem.Start();

            gamesController = new CurrentGameController();
            gamesController.Init();
            
            EntityManager.Command(new InitNetworkSystemCommand { Port = port, Key = key });
        }

        public void Update()
        {
            ServerTime += (ulong)Config.Instance.ServerTickMilliseconds;
            globalUpdateSystem.Update();
            globalUpdateSystem.LateUpdate();
        }
    }
}