using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HECSFramework.Core;

namespace Components
{
    public sealed class ClientEntitiesHolderComponent : BaseComponent
    {
        public ConcurrencyList<IEntity> ClientEntities = new ConcurrencyList<IEntity>(8);
    }
}
