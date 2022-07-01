using Commands;
using HECSFramework.Core;

namespace Systems
{
    public sealed class ServerWorldStartSystem : BaseSystem, IReactGlobalCommand<StartWorldCommand>
    {
        public void CommandGlobalReact(StartWorldCommand command)
        {
            Owner.World.GlobalUpdateSystem.Start();
            Owner.World.GlobalUpdateSystem.LateStart();
        }

        public override void InitSystem()
        {
        }
    }
}