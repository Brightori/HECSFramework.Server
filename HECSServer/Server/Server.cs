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
        private volatile bool gameloop = false, gameloopInProgress = false;
        private Config config;
        private TimeComponent time;
        public ulong ServerTime;

        public Server(int port, Config config)
        {
            this.config = config;
            var server = new Entity("Server");
            
            var dataSenderSys = new DataSenderSystem();

            var networkStaff = new Entity("NetworkStaff");
            networkStaff.AddHecsComponent(new ConnectionsHolderComponent());
            networkStaff.AddHecsSystem(dataSenderSys);

            networkStaff.Init();
            
           // HECSDebug.Init(new HECSDebugServerSide(dataSenderSys, true));

            time = new TimeComponent(config);

            server.AddHecsComponent(new ServerTagComponent());
            server.AddHecsComponent(new SyncEntitiesHolderComponent());
            server.AddHecsComponent(new AppVersionComponent());
            server.AddHecsComponent(new ConfigComponent() { Data = config });
            server.AddHecsComponent(time);
            
            
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
            
            EntityManager.Command(new InitNetworkSystemCommand { Port = port, Key = config.ServerPassword });
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

        private void GameLoop()
        {
            while (gameloop)
            {
                try
                {
                   Update();
                   int sleepTime = (int)time.TimeUntilTick;
                   if (sleepTime > 0) { Thread.Sleep(sleepTime); }
                   time.NextTick();
                }
                catch (Exception e)
                {
                    HECSDebug.LogError(e.ToString());
                }
            }
            gameloopInProgress = false;
        }

        public void StartGameLoop()
        {
            if (gameloop || gameloopInProgress)
            {
                HECSDebug.LogError("Game loop already started");
                return;
            }
            gameloop = gameloopInProgress = true;    
            Task.Run(() =>
            {
               GameLoop();
            });
        }
        public void StopGameLoop()
        {
            gameloop = false;
        }
    }
}