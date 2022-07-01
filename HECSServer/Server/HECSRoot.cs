using Commands;
using Components;
using HECSFramework.Core;
using HECSFramework.Network;
using Systems;

namespace HECSFramework.Server
{
    
    public partial class HECSSeverRoot
    {
        public HECSSeverRoot()
        {
            EntityManager entityManager = new EntityManager();
        }
    }
}