using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HECSFramework.Core;

namespace Components
{
    internal class DefaultClientContainer : EntityCoreContainer
    {
        public override string ContainerID { get; }

        protected override List<IComponent> GetComponents()
        {
            return new List<IComponent>()
            {
                new ClientTagComponent(),
                new ServerEntityTagComponent(),
                new ClientEntitiesHolderComponent(),
            };
        }

        protected override List<ISystem> GetSystems()
        {
            return new List<ISystem> 
            { 
            };
        }
    }
}
