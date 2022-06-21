using Commands;
using Components;
using HECSFramework.Core;
using HECSFramework.Network;
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
            var dataSenderSys = new DataSenderSystem();

            var networkStaff = new Entity("NetworkStaff");
            networkStaff.AddHecsComponent(new ConnectionsHolderComponent());
            networkStaff.AddHecsSystem(dataSenderSys);
            networkStaff.Init();
            
            HECSDebug.Init(new HECSDebugServerSide(dataSenderSys, true));

            server.AddHecsComponent(new ServerTagComponent());
            server.AddHecsComponent(new SyncEntitiesHolderComponent());
            server.AddHecsComponent(new AppVersionComponent());
            
            server.AddHecsSystem(new ServerNetworkSystem());
            server.AddHecsSystem(new RegisterClientSystem());
            server.AddHecsSystem(new RegisterClientEntitySystem());
            server.AddHecsSystem(new AddOrRemoveComponentSystem());
            server.Init();
            
            Start(server);

            foreach (var w in EntityManager.Worlds)
            {
                w.GlobalUpdateSystem.Start();
                w.GlobalUpdateSystem.LateStart();
            }
            
            EntityManager.Command(new InitNetworkSystemCommand { Port = port, Key = key });
        }

        partial void Start(IEntity server);

        public void Update()
        {
            ServerTime += (ulong)Config.Instance.ServerTickMilliseconds;
            foreach (var w in EntityManager.Worlds)
            {
                w.GlobalUpdateSystem.Update();
                w.GlobalUpdateSystem.LateUpdate();
                w.GlobalUpdateSystem.FinishUpdate?.Invoke();
            }
        }
    }
}