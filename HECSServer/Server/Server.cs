using Commands;
using Components;
using HECSFramework.Core;
using Systems;

namespace HECSFramework.Server
{
    /// <summary>
    /// если нам нужна проектозависимая история - добавляем парт часть, прописываем логику в методе парт
    /// </summary>
    public partial class Server
    {
        private readonly GlobalUpdateSystem globalUpdateSystem = new GlobalUpdateSystem();
        public static ulong ServerTime;

        public Server(int port, string key)
        {
            var server = new Entity("Server");
            server.AddHecsComponent(new ServerTagComponent());
            server.AddHecsComponent(new ConnectionsHolderComponent());
            server.AddHecsComponent(new SyncEntitiesHolderComponent());
            server.AddHecsComponent(new AppVersionComponent());
            
            server.AddHecsSystem(new DataSenderSystem());
            server.AddHecsSystem(new ServerNetworkSystem());
            server.AddHecsSystem(new RegisterClientSystem());
            server.AddHecsSystem(new RegisterClientEntitySystem());
            server.AddHecsSystem(new SyncEntitiesSystem());
            server.AddHecsSystem(new AddOrRemoveComponentSystem());
            server.AddHecsSystem(new SyncComponentsServerSystem());
            server.AddHecsSystem(new Debug());
            server.Init();
            
            Start(server);
            globalUpdateSystem.Start();
            EntityManager.Command(new InitNetworkSystemCommand { Port = port, Key = key });
        }

        partial void Start(IEntity server);

        public void Update()
        {
            ServerTime += (ulong)Config.Instance.ServerTickMilliseconds;
            foreach (var w in EntityManager.Worlds)
                w.GlobalUpdateSystem.Update();

            foreach (var w in EntityManager.Worlds)
                w.GlobalUpdateSystem.LateUpdate();
        }
    }
}