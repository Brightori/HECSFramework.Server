using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HECSFramework.Core;

namespace Components
{
    [Documentation(Doc.Server, Doc.HECS, "The component contains a list of entities owned by the client")]
    public sealed class ClientEntitiesHolderComponent : BaseComponent
    {
        public ConcurrencyList<IEntity> ClientEntities = new ConcurrencyList<IEntity>(8);
    }
}
