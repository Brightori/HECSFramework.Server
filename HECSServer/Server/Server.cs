using Commands;
using Components;
using HECSFramework.Core;
using HECSFramework.Network;
using System.Collections.Concurrent;
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
        private List<World> addedWrold = new List<World>();
        private List<World> worlds = new List<World>();
        private ConcurrentQueue<int> removedWrold = new ConcurrentQueue<int>();

        public ulong ServerTime { get; private set; }
        public int SystemWorldIndex { get; private set; }


        public Server(int port, Config config)
        {
            this.config = config;
            SystemWorldIndex = AddWorld().Index;
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
            
            
            server.AddHecsSystem(new ServerNetworkSystem() { Port = port, Key = config.ServerPassword });
            server.AddHecsSystem(new RegisterClientSystem());
            server.AddHecsSystem(new RegisterClientEntitySystem());
            server.AddHecsSystem(new AddOrRemoveComponentSystem());
            server.Init(SystemWorldIndex);
            
            Start(server);

           
            
        //    EntityManager.Command(new InitNetworkSystemCommand { Port = port, Key = config.ServerPassword });
        }



        public World AddWorld(params IEntityContainer[] container)
        {
            World world = EntityManager.AddWorld(container);
            lock (addedWrold)
            {
                addedWrold.Add(world);
            }
            return world;
        }

        public void RemoveWorld(int worldIdentifier)
        {
            removedWrold.Enqueue(worldIdentifier);
        }

        partial void Start(IEntity server);

        private void Update()
        {
            ServerTime += (ulong)Config.Instance.ServerTickMilliseconds;
            foreach (var w in worlds)
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
                    lock (addedWrold)
                    {
                        foreach(var world in addedWrold)
                        {
                            worlds.Add(world);
                            world.GlobalUpdateSystem.Start();
                        }
                        foreach (var world in addedWrold)
                        {
                            world.GlobalUpdateSystem.LateStart();
                        }
                        addedWrold.Clear();
                    }

                    Update();

                    while (removedWrold.TryDequeue(out int identifier))
                    {
                        worlds.RemoveAll(world =>
                        {
                            if (world.Index == identifier)
                            {
                                world.Dispose();
                                EntityManager.RemoveWorld(identifier);
                                return true;
                            }
                            return false;
                        });
                    }
                    removedWrold.Clear();


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
                throw new InvalidOperationException("Game loop already started");
            }
            gameloop = gameloopInProgress = true;
            new Thread(GameLoop)
            {
                IsBackground = true
            }
            .Start();
        }
        public void StopGameLoop()
        {
            gameloop = false;
        }
    }
}