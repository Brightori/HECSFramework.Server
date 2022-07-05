using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Components;
using HECSFramework.Core;
using Systems;

namespace HECSFramework.Server
{
    public class ServerWorldContainer : EntityCoreContainer
    {
        public override string ContainerID { get; } = "Server World";

        protected override List<IComponent> GetComponents()
        {
            return new List<IComponent>()
            {
                new ConnectionsHolderComponent(),
                new ServerTagComponent(),
                new SyncEntitiesHolderComponent(),
                new AppVersionComponent(),
                new ConfigComponent(),
                new TimeComponent(),
                new ServerTickComponent(),
                new GameLoopComponent(),
                new ServerWorldComponent(),
            };
        }

        protected override List<ISystem> GetSystems()
        {
            return new List<ISystem>()
            {
                new DataSenderSystem(),
                new ServerNetworkSystem(),
                new RegisterClientSystem(),
                new AddOrRemoveNetworkComponentSystem(),
                new ServerWorldUpdateSystem(),
                new ServerWorldStartSystem(),
            };
        }
    }
}
