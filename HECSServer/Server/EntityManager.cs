using Commands;
using Components;
using HECSFramework.Server;

namespace HECSFramework.Core
{
    [Documentation(Doc.HECS, Doc.Server, "Also have functionaly around adding server worlds and room worlds ")]
    public partial class EntityManager 
    {
        /// <summary>
        /// We create world with server world container, and init inside, and start server world inside by default
        /// </summary>
        /// <param name="config"></param>
        /// <param name="entityContainers"></param>
        /// <returns></returns>
        public static World AddServerWorld(Config config,  params IEntityContainer[] entityContainers)
        {
            var newWorld = AddWorld(new ServerWorldContainer());

            foreach (var entityContainer in entityContainers)
            {
                var e = entityContainer.GetEntityFromCoreContainer(newWorld.Index);
                newWorld.AddToInit(e);
            }

            newWorld.Init();

            newWorld.GetSingleComponent<ConfigComponent>().Data = config;

            var info = newWorld.GetSingleComponent<ServerTagComponent>().Owner.GetOrAddComponent<ConnectionInfoComponent>();
            
            info.IP = config.IPAddresss;
            info.Pass = config.ServerPassword;

            newWorld.Command(new StartWorldCommand());
            return newWorld;
        }

        public static void AddRoomWorld(World serverWorld, World roomWorld, bool needToInit = true)
        {
            var serverInfo = serverWorld.GetSingleComponent<ServerWorldComponent>();

            if (needToInit)
                roomWorld.Init();

            serverInfo.AddRoomWorld(roomWorld);
            
            roomWorld.GlobalUpdateSystem.Start();
            roomWorld.GlobalUpdateSystem.LateStart();
        }

        public static World AddWorld(params EntityCoreContainer[] entityCoreContainers)
        {
            var world = AddWorld();

            foreach (var ec in entityCoreContainers)
            {
                var entity = ec.GetEntityFromCoreContainer(world.Index);
                world.AddToInit(entity);
            }

            return world;
        }

        public static World AddWorld(params IEntityContainer[] entityCoreContainers)
        {
            var world = AddWorld();

            foreach (var ec in entityCoreContainers)
            {
                var entity = ec.GetEntityFromCoreContainer(world.Index);
                world.AddToInit(entity);
            }

            return world;
        }
    }
}
