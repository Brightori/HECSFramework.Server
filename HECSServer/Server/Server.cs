using Commands;
using Components;
using HECSFramework.Core;
using Systems;

namespace HECSFramework.Server
{
    public class Server
    {
        private readonly GlobalUpdateSystem globalUpdateSystem = new GlobalUpdateSystem();
        public static ulong ServerTime;

        public Server(int port, string key)
        {
            var server = new Entity("Server");
            server.AddHecsComponent(new ServerTagComponent());
            server.AddHecsComponent(new ConnectionsHolderComponent());
            server.AddHecsSystem(new ServerNetworkSystem());
            server.AddHecsSystem(new Debug());
            server.Init();
            
            globalUpdateSystem.Start();

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